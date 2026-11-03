using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Publisher.Application;
using Publisher.Infrastructure.Repositories;
using Publisher.Infrastructure.Settings;


namespace Publisher.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Registrar IDbConnection
        var connectionString = configuration.GetConnectionString("PublisherDb");
        services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));

        // Obtener Configuracion de RabbitMQ
        services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));

        //Obtener Configuracion de SeaweedFS
        services.Configure<SeaweedFSSettings>(configuration.GetSection("SeaweedFS"));
        services.AddHttpClient<SeaweedFSFileRepository>(); // HttpClient tipado

        // Registrar repositorios
        services.AddScoped<IExcelFileRepository, ExcelFileRepository>();
        services.AddScoped<IFileRepository, SeaweedFSFileRepository>();

        // 4. RabbitMQ Real (Reemplazamos el simulado)
        services.AddScoped<IMessagePublisher, RabbitMQMessagePublisher>();

        return services;
    }
}
