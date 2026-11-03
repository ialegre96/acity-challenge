using MediatR;

namespace Publisher.Application.Queries;

public class GetUploadStatusQueryHandler : IRequestHandler<GetUploadStatusQuery, UploadStatusResponse>
{
    private readonly IExcelFileRepository _excelFileRepository;

    public GetUploadStatusQueryHandler(IExcelFileRepository excelFileRepository)
    {
        _excelFileRepository = excelFileRepository;
    }

    public async Task<UploadStatusResponse> Handle(GetUploadStatusQuery request, CancellationToken cancellationToken)
    {
        var file = await _excelFileRepository.GetByIdAsync(request.FileId, cancellationToken);

        if (file == null)
        {
            throw new KeyNotFoundException($"No se Encontro el Archivo con Id {request.FileId}");
        }

        return new UploadStatusResponse
        {
            FileId = file.Id,
            FileName = file.FileName,
            FilePath = file.FilePath,
            Status = file.Status,
            CreatedAt = file.CreatedAt,
            ProcessedAt = file.ProcessedAt
        };
    }
}
