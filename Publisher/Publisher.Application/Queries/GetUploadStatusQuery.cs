using MediatR;

namespace Publisher.Application.Queries;

public class GetUploadStatusQuery : IRequest<UploadStatusResponse>
{
    public Guid FileId { get; set; }
}
