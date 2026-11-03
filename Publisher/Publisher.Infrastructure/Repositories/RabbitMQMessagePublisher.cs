using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Publisher.Application;
using Publisher.Infrastructure.Settings;
using RabbitMQ.Client;

namespace Publisher.Infrastructure.Repositories;

public class RabbitMQMessagePublisher : IMessagePublisher
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQMessagePublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQMessagePublisher(
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMQMessagePublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // 1. Crear la conexión a RabbitMQ
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // 2. Declarar la cola (si no existe, la crea)
        _channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _logger.LogInformation("✅ Conexión a RabbitMQ establecida. Cola: {QueueName}", _settings.QueueName);
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        var messageJson = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(messageJson);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            exchange: "",
            routingKey: _settings.QueueName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "📨 [RabbitMQ] Mensaje publicado en cola {QueueName}: {Message}",
            _settings.QueueName,
            messageJson);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
