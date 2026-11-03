
using Publisher.Domain.Entities;

namespace Publisher.Application;

public interface IExcelFileRepository
{
    Task CreateAsync(ExcelFile file, CancellationToken cancellationToken);
    Task UpdateAsync(ExcelFile file, CancellationToken cancellationToken);
    Task<ExcelFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<ExcelFile>> GetAllAsync(CancellationToken cancellationToken);

}
