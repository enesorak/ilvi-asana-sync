# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY src/Ilvi.Asana.Domain/*.csproj ./src/Ilvi.Asana.Domain/
COPY src/Ilvi.Asana.Application/*.csproj ./src/Ilvi.Asana.Application/
COPY src/Ilvi.Asana.Infrastructure/*.csproj ./src/Ilvi.Asana.Infrastructure/
COPY src/Ilvi.Asana.Web/*.csproj ./src/Ilvi.Asana.Web/

# Restore
RUN dotnet restore

# Copy source
COPY . .

# Build
WORKDIR /src/src/Ilvi.Asana.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install ImageSharp dependencies
RUN apt-get update && apt-get install -y \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create attachments directory
RUN mkdir -p /app/attachments

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Ilvi.Asana.Web.dll"]
