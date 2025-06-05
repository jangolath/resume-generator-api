# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy main project file and restore dependencies
COPY ResumeGenerator.API.csproj ./
RUN dotnet restore ResumeGenerator.API.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish ResumeGenerator.API.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install system dependencies for better performance and monitoring
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create directories with proper permissions
RUN mkdir -p /app/logs && \
    chown -R appuser:appuser /app

# Copy the published app
COPY --from=build-env /app/out .

# Change to non-root user
USER appuser

# Expose the port that the application listens on
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "ResumeGenerator.API.dll"]