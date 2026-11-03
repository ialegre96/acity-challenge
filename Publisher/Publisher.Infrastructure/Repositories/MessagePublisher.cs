using Publisher.Application;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Publisher.Infrastructure.Repositories;

public class MessagePublisher : IMessagePublisher
{
    private readonly ILogger<MessagePublisher> _logger;

    public MessagePublisher(ILogger<MessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        // Simular la publicación a RabbitMQ con un log estructurado
        var messageJson = JsonSerializer.Serialize(message);

        _logger.LogInformation(
            "📨 [RabbitMQ Simulado] Publicando mensaje de tipo {MessageType} con contenido: {MessageContent}",
            typeof(T).Name,
            messageJson);

        // En un caso real, aquí iría:
        // await _channel.BasicPublishAsync(...);

        return Task.CompletedTask;
    }
}
