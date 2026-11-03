using System.Text;
using AuthService.Application.Commands;
using AuthService.Application.Interfaces;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly));

// 2. Registrar Dependencias de la Infraestructura (Como no creamos las carpetas .Domain, .App, .Infra separadas para evitar errores, lo hacemos manual)
builder.Services.AddScoped<IUserRepository, MockUserRepository>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

// 3. Controladores y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. JWT
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
