
using MediatR;
using Publisher.Domain.Entities;
using Publisher.Domain.Enums;
namespace Publisher.Application.Commands;

public class UploadExcelCommandHandler : IRequestHandler<UploadExcelCommand, UploadExcelResponse>
{
    private readonly IFileRepository _fileRepository;
    private readonly IExcelFileRepository _excelFileRepository;
    private readonly IMessagePublisher _messagePublisher;

    public UploadExcelCommandHandler(IFileRepository fileRepository, IExcelFileRepository excelFileRepository, IMessagePublisher messagePublisher)
    {
        _fileRepository = fileRepository;
        _excelFileRepository = excelFileRepository;
        _messagePublisher = messagePublisher;
    }

    public async Task<UploadExcelResponse> Handle(UploadExcelCommand request, CancellationToken cancellationToken)
    {
        var fileId = Guid.NewGuid();
        var excelFile = new ExcelFile
        {
            Id = fileId,
            FileName = request.FileName!,
            FilePath = string.Empty,
            Status = FileStatus.Pendiente,
            CreatedAt = DateTime.UtcNow
        };

        await _excelFileRepository.CreateAsync(excelFile, cancellationToken);

        var filePath = await _fileRepository.UploadAsync(request.FileContent, request.FileName, cancellationToken);

        excelFile.FilePath = filePath;

        await _excelFileRepository.UpdateAsync(excelFile, cancellationToken);

        var message = new { FileId = fileId, FilePath = filePath, FileName = request.FileName };

        await _messagePublisher.PublishAsync(message, cancellationToken);

        return new UploadExcelResponse { FileId = fileId };
    }


}
