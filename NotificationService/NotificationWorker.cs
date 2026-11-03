using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Messages;
using NotificationService.Infrastructure.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService;

public class NotificationWorker : BackgroundService
{
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly IEmailService _emailService;
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(
        IOptions<RabbitMQSettings> rabbitSettings,
        IEmailService emailService,
        IDbConnection dbConnection,
        ILogger<NotificationWorker> logger)
    {
        _rabbitSettings = rabbitSettings.Value;
        _emailService = emailService;
        _dbConnection = dbConnection;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitSettings.HostName,
            Port = _rabbitSettings.Port,
            UserName = _rabbitSettings.UserName,
            Password = _rabbitSettings.Password
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(queue: _rabbitSettings.NotificationsQueueName, durable: true, exclusive: false, autoDelete: false);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("✅ NotificationService conectado. Escuchando cola: {Queue}", _rabbitSettings.NotificationsQueueName);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);

            try
            {
                var message = JsonSerializer.Deserialize<NotificacionMessage>(messageJson);
                if (message == null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                _logger.LogInformation("📨 Procesando notificación para archivo: {FileId}", message.FileId);

                // 1. Enviar el correo
                var userEmail = "usuario@example.com"; // TODO: Obtener de BD
                var subject = $"Carga Masiva Finalizada - {message.FileName}";
                var bodyHtml = $@"
                    <h2>Procesamiento Completado</h2>
                    <p>El archivo <b>{message.FileName}</b> ha sido procesado.</p>
                    <ul>
                        <li>Total Registros: {message.TotalRegistros}</li>
                        <li>Exitosos: {message.RegistrosExitosos}</li>
                        <li>Fallidos: {message.RegistrosFallidos}</li>
                    </ul>";

                await _emailService.SendEmailAsync(userEmail, subject, bodyHtml, stoppingToken);

                // 2. Actualizar estado a Notificado (4)
                if (_dbConnection.State != ConnectionState.Open)
                    _dbConnection.Open();

                await _dbConnection.ExecuteAsync(
                    "sp_UpdateExcelFile",
                    new { Id = message.FileId, FilePath = (string?)null, Status = 4, ProcessedAt = (DateTime?)null, Periodo = (DateTime?)null },
                    commandType: CommandType.StoredProcedure);

                _logger.LogInformation("✅ Estado actualizado a Notificado para archivo: {FileId}", message.FileId);

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando notificación");
                channel.BasicNack(ea.DeliveryTag, false, requeue: true);
            }
        };

        channel.BasicConsume(queue: _rabbitSettings.NotificationsQueueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }
}
