using NotificationService;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.Settings;

var builder = Host.CreateApplicationBuilder(args);

// 1. Configuraciones
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// 2. Base de Datos
var connectionString = builder.Configuration.GetConnectionString("PublisherDb");
builder.Services.AddScoped<System.Data.IDbConnection>(sp => new Microsoft.Data.SqlClient.SqlConnection(connectionString));

// 3. Servicios
builder.Services.AddScoped<IEmailService, EmailService>();

// 4. Worker
builder.Services.AddHostedService<NotificationWorker>();

var host = builder.Build();
host.Run();
