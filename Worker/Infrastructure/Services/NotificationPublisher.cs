using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Worker.Application.Interfaces;
using Worker.Application.Messages;
using RabbitMQ.Client;
using Worker.Infrastructure.Settings;

namespace Worker.Infrastructure.Services;

public class NotificationPublisher : INotificationPublisher, IDisposable
{
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly ILogger<NotificationPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public NotificationPublisher(
        IOptions<RabbitMQSettings> rabbitSettings,
        ILogger<NotificationPublisher> logger)
    {
        _rabbitSettings = rabbitSettings.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _rabbitSettings.HostName,
            Port = _rabbitSettings.Port,
            UserName = _rabbitSettings.UserName,
            Password = _rabbitSettings.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: _rabbitSettings.NotificationsQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _logger.LogInformation("✅ NotificationPublisher conectado a RabbitMQ");
    }

    public Task PublishAsync(NotificacionMessage message, CancellationToken cancellationToken)
    {
        var messageJson = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(messageJson);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            exchange: "",
            routingKey: _rabbitSettings.NotificationsQueueName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("📨 Notificación publicada para archivo {FileId}", message.FileId);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
