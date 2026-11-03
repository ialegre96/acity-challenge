using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worker.Application.Interfaces;
using Worker.Infrastructure.Settings;

namespace Worker.Infrastructure.Services;

public class SeaweedFSClient : ISeaweedFSClient
{
    private readonly SeaweedFSSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SeaweedFSClient> _logger;

    public SeaweedFSClient(
        IOptions<SeaweedFSSettings> settings,
        HttpClient httpClient,
        ILogger<SeaweedFSClient> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Stream> DownloadAsync(string fileUrl, CancellationToken cancellationToken)
    {
        _logger.LogInformation("⬇️ Descargando archivo de SeaweedFS: {Url}", fileUrl);

        var response = await _httpClient.GetAsync(fileUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("✅ Archivo descargado exitosamente");

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}
