using MediatR;

namespace Publisher.Application.Commands;

public class UploadExcelCommand : IRequest<UploadExcelResponse>
{
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public Stream? FileContent { get; set; }
}
