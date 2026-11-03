using System.Data;
using Dapper;
using Publisher.Application;
using Publisher.Domain.Entities;

namespace Publisher.Infrastructure.Repositories;

public class ExcelFileRepository : IExcelFileRepository
{
    private readonly IDbConnection _connection;

    public ExcelFileRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task CreateAsync(ExcelFile file, CancellationToken cancellationToken)
    {
        var procedure = "sp_CreateExcelFile";

        var parameters = new DynamicParameters();

        parameters.Add("@Id", file.Id);
        parameters.Add("@FileName", file.FileName);
        parameters.Add("@FilePath", file.FilePath);
        parameters.Add("@Status", file.Status);
        parameters.Add("@CreatedAt", file.CreatedAt);

        var result = await _connection.ExecuteAsync(
            procedure, parameters, commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateAsync(ExcelFile file, CancellationToken cancellationToken)
    {
        var procedure = "sp_UpdateExcelFile";

        var parameters = new DynamicParameters();
        parameters.Add("@Id", file.Id);
        parameters.Add("@FilePath", file.FilePath);
        parameters.Add("@Status", (int)file.Status);
        parameters.Add("@ProcessedAt", file.ProcessedAt);

        await _connection.ExecuteAsync(
            procedure,
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<ExcelFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var procedure = "sp_GetExcelFileById";

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);

        return await _connection.QueryFirstOrDefaultAsync<ExcelFile>(procedure, parameters, commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<ExcelFile>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sql = "SELECT * FROM ExcelFiles ORDER BY CreatedAt DESC";
        return await _connection.QueryAsync<ExcelFile>(sql);
    }
}
