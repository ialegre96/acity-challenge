# ============================================
# STAGE 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto (rutas relativas a la RAÍZ AcityChallenge)
COPY ["Publisher/Publisher.Api/Publisher.Api.csproj", "Publisher/Publisher.Api/"]
COPY ["Publisher/Publisher.Application/Publisher.Application.csproj", "Publisher/Publisher.Application/"]
COPY ["Publisher/Publisher.Domain/Publisher.Domain.csproj", "Publisher/Publisher.Domain/"]
COPY ["Publisher/Publisher.Infrastructure/Publisher.Infrastructure.csproj", "Publisher/Publisher.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "Publisher/Publisher.Api/Publisher.Api.csproj"

# Copiar todo el código fuente
COPY ["Publisher/", "Publisher/"]

# Compilar la aplicación
WORKDIR "/src/Publisher/Publisher.Api"
RUN dotnet build "Publisher.Api.csproj" -c Release -o /app/build

# ============================================
# STAGE 2: Publish
# ============================================
FROM build AS publish
RUN dotnet publish "Publisher.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# STAGE 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copiar los archivos publicados
COPY --from=publish /app/publish .

# Exponer el puerto
EXPOSE 8080

# Variables de entorno
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Comando de inicio
ENTRYPOINT ["dotnet", "Publisher.Api.dll"]
