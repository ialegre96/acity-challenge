using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Worker.Application.Interfaces;
using Worker.Application.Messages;
using Worker.Infrastructure.Services;
using Worker.Infrastructure.Settings;

namespace Worker;

public class MassiveLoadWorker : BackgroundService
{
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly SeaweedFSSettings _seaweedSettings;
    private readonly string _connectionString;
    private readonly ILoggerFactory _loggerFactory; // ✅ Cambiado de ILogger<Worker> a ILoggerFactory

    public MassiveLoadWorker(
        IConfiguration configuration,
        ILoggerFactory loggerFactory) // ✅ Cambiado el parámetro
    {
        _rabbitSettings = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>()!;
        _seaweedSettings = configuration.GetSection("SeaweedFS").Get<SeaweedFSSettings>()!;
        _connectionString = configuration.GetConnectionString("PublisherDb")!;
        _loggerFactory = loggerFactory; // ✅ Guardar la factory
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

        // Declarar cola de carga masiva
        channel.QueueDeclare(
            queue: _rabbitSettings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Declarar cola de notificaciones
        channel.QueueDeclare(
            queue: _rabbitSettings.NotificationsQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _loggerFactory.CreateLogger<MassiveLoadWorker>().LogInformation("✅ Worker conectado. Escuchando cola: {Queue}", _rabbitSettings.QueueName);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);

            try
            {
                var message = JsonSerializer.Deserialize<CargaMasivaMessage>(messageJson);
                if (message == null)
                {
                    _loggerFactory.CreateLogger<MassiveLoadWorker>().LogWarning("⚠️ Mensaje nulo o inválido");
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                _loggerFactory.CreateLogger<MassiveLoadWorker>().LogInformation("📨 Procesando archivo: {FileId}", message.FileId);

                // 1. Descargar archivo de SeaweedFS
                using var httpClient = new HttpClient();
                var seaweedClient = new SeaweedFSClient(
                    Microsoft.Extensions.Options.Options.Create(_seaweedSettings),
                    httpClient,
                    _loggerFactory.CreateLogger<SeaweedFSClient>()); // ✅ Logger tipado

                using var fileStream = await seaweedClient.DownloadAsync(message.FilePath, stoppingToken);

                // 2. Procesar Excel
                using var dbConnection = new SqlConnection(_connectionString);
                await dbConnection.OpenAsync(stoppingToken);

                var processor = new ExcelProcessor(dbConnection, _loggerFactory.CreateLogger<ExcelProcessor>()); // ✅ Logger tipado
                var result = await processor.ProcessAsync(message.FileId, fileStream, stoppingToken);

                // 3. Si fue exitoso, publicar notificación
                if (result.Success)
                {
                    var notificationMessage = new NotificacionMessage
                    {
                        FileId = message.FileId,
                        FileName = message.FileName,
                        FechaFin = DateTime.UtcNow,
                        TotalRegistros = result.TotalRows,
                        RegistrosExitosos = result.SuccessfulRows,
                        RegistrosFallidos = result.FailedRows.Count
                    };

                    var notificationPublisher = new NotificationPublisher(
                        Microsoft.Extensions.Options.Options.Create(_rabbitSettings),
                        _loggerFactory.CreateLogger<NotificationPublisher>()); // ✅ Logger tipado

                    await notificationPublisher.PublishAsync(notificationMessage, stoppingToken);

                    _loggerFactory.CreateLogger<MassiveLoadWorker>().LogInformation("📤 Notificación publicada para archivo {FileId}", message.FileId);
                }
                else
                {
                    _loggerFactory.CreateLogger<MassiveLoadWorker>().LogError("❌ Falló el procesamiento: {Error}", result.ErrorMessage);
                }

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _loggerFactory.CreateLogger<MassiveLoadWorker>().LogError(ex, "❌ Error procesando mensaje");
                channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(
            queue: _rabbitSettings.QueueName,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }
}
