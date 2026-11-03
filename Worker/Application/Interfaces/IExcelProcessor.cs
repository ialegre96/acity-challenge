using Worker.Application.Messages;

namespace Worker.Application.Interfaces;

public interface IExcelProcessor
{
    Task<ProcessResult> ProcessAsync(Guid fileId, Stream excelStream, CancellationToken cancellationToken);
}

public class ProcessResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int ExistingRecords { get; set; }
    public int SkippedRows { get; set; }
    public List<FailedRow> FailedRows { get; set; } = new();
}

public class FailedRow
{
    public int Row { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
