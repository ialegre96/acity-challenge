# ============================================
# STAGE 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copiar archivos de proyecto (rutas relativas a la raíz AcityChallenge)
COPY ["Worker/Worker.csproj", "Worker/"]
COPY ["Publisher/Publisher.Domain/Publisher.Domain.csproj", "Publisher/Publisher.Domain/"]
COPY ["Publisher/Publisher.Application/Publisher.Application.csproj", "Publisher/Publisher.Application/"]
COPY ["Publisher/Publisher.Infrastructure/Publisher.Infrastructure.csproj", "Publisher/Publisher.Infrastructure/"]

# 2. Restaurar dependencias
RUN dotnet restore "Worker/Worker.csproj"

# 3. Copiar todo el código fuente
COPY ["Worker/", "Worker/"]
COPY ["Publisher/Publisher.Domain/", "Publisher/Publisher.Domain/"]
COPY ["Publisher/Publisher.Application/", "Publisher/Publisher.Application/"]
COPY ["Publisher/Publisher.Infrastructure/", "Publisher/Publisher.Infrastructure/"]

# 4. Compilar la aplicación
WORKDIR "/src/Worker"
RUN dotnet build "Worker.csproj" -c Release -o /app/build

# ============================================
# STAGE 2: Publish
# ============================================
FROM build AS publish
RUN dotnet publish "Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# STAGE 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Copiar los archivos publicados
COPY --from=publish /app/publish .

# Variables de entorno
ENV DOTNET_ENVIRONMENT=Production

# Comando de inicio
ENTRYPOINT ["dotnet", "Worker.dll"]
