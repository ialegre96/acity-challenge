using ApiGateway.Extensions;
using ApiGateway.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar YARP (Enrutamiento)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Configurar Autenticación (Usando la Extensión)
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// 3. Pipeline de Middlewares
app.UseAuthentication();
app.UseAuthorization();

// 4. Validación personalizada (Usando el Middleware)
app.UseMiddleware<TokenValidationMiddleware>();

// 5. Mapear YARP
app.MapReverseProxy();

app.Run();
