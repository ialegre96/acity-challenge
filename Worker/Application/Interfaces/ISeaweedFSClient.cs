namespace Worker.Application.Interfaces;

public interface ISeaweedFSClient
{
    Task<Stream> DownloadAsync(string fileUrl, CancellationToken cancellationToken);
}
