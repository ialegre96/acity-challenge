using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Publisher.Api.Middlewares;
using Publisher.Application;
using Publisher.Infrastructure.Repositories;
using Publisher.Infrastructure.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar MediatR (CQRS)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(Publisher.Application.Commands.UploadExcelCommand).Assembly));

// 2. Registrar Controladores (Tu tarea: completa esta línea)
builder.Services.AddControllers();

// 3. Swagger para documentación (Obligatorio en el reto)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar el repositorio de ExcelFiles
builder.Services.AddInfrastructure(builder.Configuration);

var jwtKey = "Esta_Es_Una_Clave_Secreta_Muy_Larga_Para_El_Reto_Acity_2025";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "AcityChallenge",
            ValidAudience = "AcityChallengeUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });


var app = builder.Build();

// 4. Middleware de excepciones (DEBE ir antes que todo lo demás)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 5. Configuración de entorno
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
