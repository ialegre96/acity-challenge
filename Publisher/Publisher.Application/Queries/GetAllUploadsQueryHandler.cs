using MediatR;

namespace Publisher.Application.Queries;

public class GetAllUploadsQueryHandler : IRequestHandler<GetAllUploadsQuery, IEnumerable<UploadStatusResponse>>
{
    private readonly IExcelFileRepository _excelFileRepository;

    public GetAllUploadsQueryHandler(IExcelFileRepository excelFileRepository)
    {
        _excelFileRepository = excelFileRepository;
    }

    public async Task<IEnumerable<UploadStatusResponse>> Handle(GetAllUploadsQuery request, CancellationToken cancellationToken)
    {
        var files = await _excelFileRepository.GetAllAsync(cancellationToken);

        return files.Select(f => new UploadStatusResponse
        {
            FileId = f.Id,
            FileName = f.FileName,
            FilePath = f.FilePath,
            Status = f.Status,
            CreatedAt = f.CreatedAt,
            ProcessedAt = f.ProcessedAt
        });
    }
}
