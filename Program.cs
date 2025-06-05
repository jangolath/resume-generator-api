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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ResumeGeneratorContext>();
    await context.Database.EnsureCreatedAsync();
}

try
{
    Log.Information("Starting Resume Generator API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}