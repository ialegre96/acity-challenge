using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Publisher.Domain.Enums;
using Worker.Application.Interfaces;

namespace Worker.Infrastructure.Services;

public class ExcelProcessor : IExcelProcessor
{
    private readonly IDbConnection _connection;
    private readonly ILogger<ExcelProcessor> _logger;

    public ExcelProcessor(IDbConnection connection, ILogger<ExcelProcessor> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<ProcessResult> ProcessAsync(Guid fileId, Stream excelStream, CancellationToken cancellationToken)
    {
        var result = new ProcessResult();
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage(excelStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            await UpdateStatusAsync(fileId, FileStatus.Fallido, null, cancellationToken);
            return new ProcessResult { Success = false, ErrorMessage = "El archivo Excel no contiene hojas" };
        }

        // =========================================================
        // 1. LEER Y VALIDAR EL PERIODO
        // =========================================================
        var periodoCell = worksheet.Cells[2, 1].Text;
        if (string.IsNullOrWhiteSpace(periodoCell) || !DateOnly.TryParse(periodoCell, out var periodo))
        {
            await UpdateStatusAsync(fileId, FileStatus.Fallido, null, cancellationToken);
            return new ProcessResult { Success = false, ErrorMessage = "Formato de Periodo inválido o vacío en la fila 2." };
        }

        var periodoDate = periodo.ToDateTime(TimeOnly.MinValue);

        // =========================================================
        // 2. VALIDACIÓN DE DUPLICIDAD POR PERIODO (Consultando ExcelFiles)
        // =========================================================
        var estadoExistente = await _connection.QueryFirstOrDefaultAsync<byte?>(
            @"SELECT TOP 1 Status FROM ExcelFiles
              WHERE Periodo = @Periodo
              AND Id != @FileId",
            new { Periodo = periodoDate, FileId = fileId });

        if (estadoExistente.HasValue)
        {
            var statusEnum = (FileStatus)estadoExistente.Value;

            // Regla: Si está Cargado, Finalizado o Notificado -> Rechazar
            if (statusEnum == FileStatus.Cargado || statusEnum == FileStatus.Finalizado || statusEnum == FileStatus.Notificado)
            {
                await UpdateStatusAsync(fileId, FileStatus.Fallido, null, cancellationToken);
                return new ProcessResult { Success = false, ErrorMessage = $"Rechazado: Ya existe una carga en estado '{statusEnum}' para el periodo {periodoDate:yyyy-MM-dd}." };
            }

            // Regla: Si está Pendiente o EnProceso -> Bloquear
            if (statusEnum == FileStatus.Pendiente || statusEnum == FileStatus.EnProceso)
            {
                await UpdateStatusAsync(fileId, FileStatus.Fallido, null, cancellationToken);
                return new ProcessResult { Success = false, ErrorMessage = $"Bloqueado: Ya hay una carga en proceso ('{statusEnum}') para el periodo {periodoDate:yyyy-MM-dd}." };
            }
        }

        // Si pasa la validación, actualizamos a EnProceso y guardamos el Periodo en la cabecera
        await UpdateStatusAsync(fileId, FileStatus.EnProceso, periodoDate, cancellationToken);

        try
        {
            var processedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            result.TotalRows = rowCount - 1;

            var rowsToInsert = new List<dynamic>();

            for (int row = 2; row <= rowCount; row++)
            {
                if (string.IsNullOrWhiteSpace(worksheet.Cells[row, 1].Text) &&
                    string.IsNullOrWhiteSpace(worksheet.Cells[row, 2].Text))
                {
                    result.SkippedRows++;
                    continue;
                }

                var codigoProducto = worksheet.Cells[row, 2].Text?.Trim();
                var nombreProducto = worksheet.Cells[row, 3].Text?.Trim();
                var precioText = worksheet.Cells[row, 4].Text?.Trim();

                if (string.IsNullOrWhiteSpace(codigoProducto))
                {
                    result.SkippedRows++;
                    result.FailedRows.Add(new FailedRow { Row = row, CodigoProducto = "N/A", Reason = "Código de producto vacío" });
                    continue;
                }

                if (!processedCodes.Add(codigoProducto))
                {
                    result.ExistingRecords++;
                    result.FailedRows.Add(new FailedRow { Row = row, CodigoProducto = codigoProducto, Reason = "Código duplicado dentro del mismo archivo" });
                    continue;
                }

                var exists = await _connection.ExecuteScalarAsync<bool>(
                    @"SELECT COUNT(1) FROM ProductRecords
                      WHERE CodigoProducto = @Codigo AND Periodo = @Periodo",
                    new { Codigo = codigoProducto, Periodo = periodoDate });

                if (exists)
                {
                    result.ExistingRecords++;
                    result.FailedRows.Add(new FailedRow { Row = row, CodigoProducto = codigoProducto, Reason = $"Código ya existe en BD para el periodo" });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(nombreProducto)) nombreProducto = "Sin nombre";
                if (!decimal.TryParse(precioText, out var precio)) precio = 0;

                rowsToInsert.Add(new
                {
                    Id = Guid.NewGuid(),
                    ExcelFileId = fileId,
                    Periodo = periodoDate,
                    CodigoProducto = codigoProducto,
                    NombreProducto = nombreProducto,
                    Precio = precio
                });

                result.SuccessfulRows++;
            }

            // Inserción Masiva
            if (rowsToInsert.Any())
            {
                var sqlInsert = @"
                    INSERT INTO ProductRecords (Id, ExcelFileId, Periodo, CodigoProducto, NombreProducto, Precio)
                    VALUES (@Id, @ExcelFileId, @Periodo, @CodigoProducto, @NombreProducto, @Precio)";

                await _connection.ExecuteAsync(sqlInsert, rowsToInsert);
            }

            // Auditoría de Fallidos
            if (result.FailedRows.Any())
            {
                var sqlAuditoria = @"
                    INSERT INTO AuditoriaFallidos (ExcelFileId, Fila, CodigoProducto, Motivo)
                    VALUES (@ExcelFileId, @Fila, @CodigoProducto, @Motivo)";

                var fallidosToInsert = result.FailedRows.Select(f => new
                {
                    ExcelFileId = fileId,
                    Fila = f.Row,
                    CodigoProducto = f.CodigoProducto,
                    Motivo = f.Reason
                });

                await _connection.ExecuteAsync(sqlAuditoria, fallidosToInsert);
            }

            result.Success = true;
            await UpdateStatusAsync(fileId, FileStatus.Cargado, periodoDate, cancellationToken);
            await UpdateStatusAsync(fileId, FileStatus.Finalizado, periodoDate, cancellationToken, DateTime.UtcNow);

            _logger.LogInformation("✅ Procesamiento completado. Éxitos: {Success}, Fallos: {Failed}", result.SuccessfulRows, result.FailedRows.Count);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "❌ Error procesando archivo {FileId}", fileId);
            await UpdateStatusAsync(fileId, FileStatus.Fallido, null, cancellationToken);
        }

        return result;
    }

    // Agregamos el parámetro period para que se guarde en la cabecera del archivo
    private async Task UpdateStatusAsync(Guid fileId, FileStatus status, DateTime? period, CancellationToken cancellationToken, DateTime? processedAt = null)
    {
        await _connection.ExecuteAsync(
            "sp_UpdateExcelFile",
            new
            {
                Id = fileId,
                FilePath = (string?)null,
                Status = (int)status,
                ProcessedAt = processedAt,
                Periodo = period // Se pasa al SP
            },
            commandType: CommandType.StoredProcedure);
    }
}
