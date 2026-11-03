using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Publisher.Application;
using Publisher.Infrastructure.Settings;

namespace Publisher.Infrastructure.Repositories;

public class SeaweedFSFileRepository : IFileRepository
{
    private readonly SeaweedFSSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SeaweedFSFileRepository> _logger;

    public SeaweedFSFileRepository(
        IOptions<SeaweedFSSettings> settings,
        HttpClient httpClient,
        ILogger<SeaweedFSFileRepository> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        // 1. Generar nombre único
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        // 2. Construir URLs
        var bucketUrl = $"{_settings.BaseUrl}/{_settings.BucketName}";
        var objectUrl = $"{bucketUrl}/{uniqueFileName}";

        // 3. ✅ Asegurar que el bucket existe en SeaweedFS (S3 API)
        try
        {
            // Un PUT vacío al nombre del bucket lo crea si no existe
            using var emptyContent = new ByteArrayContent(Array.Empty<byte>());
            await _httpClient.PutAsync(bucketUrl, emptyContent, cancellationToken);
        }
        catch
        {
            // Si ya existe, puede fallar, pero lo ignoramos para continuar
            _logger.LogDebug("El bucket {BucketName} ya existe.", _settings.BucketName);
        }

        // 4. Preparar el contenido del archivo
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // 5. Subir el archivo con PUT
        var response = await _httpClient.PutAsync(objectUrl, content, cancellationToken);

        // 6. Validación robusta de errores
        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("❌ Error al subir a SeaweedFS. Status: {StatusCode}. Detalles: {Details}", response.StatusCode, errorDetails);
            throw new HttpRequestException($"Fallo al subir el archivo a SeaweedFS: {response.StatusCode}");
        }

        _logger.LogInformation("✅ Archivo subido exitosamente a SeaweedFS: {FileUrl}", objectUrl);

        return objectUrl;
    }
}
