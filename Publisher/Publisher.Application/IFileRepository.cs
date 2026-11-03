namespace Publisher.Application;

public interface IFileRepository
{
    Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken);

}
