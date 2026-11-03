using System.Net;
using System.Text.Json;

namespace Publisher.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Continúa con el siguiente middleware o controlador en el pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // 1. Loguear el error con el logger inyectado
            _logger.LogError(ex, "Ocurrió un error no manejado: {Message}", ex.Message);

            // 2. Limpiar la respuesta actual (por si algo ya se escribió)
            context.Response.Clear();

            // 3. Establecer código HTTP 500 y el tipo de contenido
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            // 4. Retornar un JSON estandarizado con el error
            var errorResponse = new
            {
                statusCode = context.Response.StatusCode,
                message = "Ocurrió un error interno del servidor. Por favor, intente nuevamente.",
                // En desarrollo puedes incluir ex.Message, en producción NO (por seguridad)
                details = ex.Message
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
