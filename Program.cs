using ResumeGenerator.API.Data;
using ResumeGenerator.API.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/resume-generator-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services using extension methods
builder.Services.AddApplicationConfiguration(builder.Configuration);
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddHttpClientConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddBackgroundServices();
builder.Services.AddApiConfiguration();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddHealthChecksConfiguration(builder.Configuration);
builder.Services.AddSwaggerConfiguration();
builder.Services.AddCachingConfiguration();

var app = builder.Build();

// Configure the application pipeline
app.ConfigureApplication();

try
{
    Log.Information("Starting Resume Generator API");
    
    using (var scope = app.Services.CreateScope())
    {
        Log.Information("Creating database scope...");
        var context = scope.ServiceProvider.GetRequiredService<ResumeGeneratorContext>();
        
        Log.Information("Ensuring database is created...");
        await context.Database.EnsureCreatedAsync();
        
        Log.Information("Database initialization completed");
    }
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}