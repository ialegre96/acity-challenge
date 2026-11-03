namespace ApiGateway.Middlewares;

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Si la ruta NO es de autenticación, exigimos que el usuario esté autenticado
        if (!context.Request.Path.StartsWithSegments("/auth"))
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "No autorizado. Falta el Token JWT." });
                return;
            }
        }

        // Continuar con el siguiente middleware en el pipeline (YARP)
        await _next(context);
    }
}
