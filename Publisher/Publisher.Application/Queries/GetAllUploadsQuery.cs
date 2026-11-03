using MediatR;

namespace Publisher.Application.Queries;

public class GetAllUploadsQuery : IRequest<IEnumerable<UploadStatusResponse>>
{
}
