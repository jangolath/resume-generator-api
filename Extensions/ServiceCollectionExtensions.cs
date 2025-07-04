using Microsoft.EntityFrameworkCore;
using ResumeGenerator.API.Configuration;
using ResumeGenerator.API.Data;
using ResumeGenerator.API.Services.BackgroundServices;
using ResumeGenerator.API.Services.Implementation;
using ResumeGenerator.API.Services.Interfaces;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ResumeGenerator.API.Extensions;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add database configuration
    /// </summary>
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddDbContext<ResumeGeneratorContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ResumeGeneratorContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });

            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
        _ = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"] ?? string.Empty))
            };
        });

            return services;
        }

    /// <summary>
    /// Add application configuration
    /// </summary>
    public static IServiceCollection AddApplicationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration sections
        services.Configure<ClaudeApiSettings>(
            configuration.GetSection("ClaudeApi"));
        services.Configure<OpenAISettings>(
            configuration.GetSection("OpenAI"));
        services.Configure<GoogleDocsSettings>(
            configuration.GetSection("GoogleDocs"));
        services.Configure<ApplicationSettings>(
            configuration.GetSection("Application"));
        services.Configure<RateLimitSettings>(
            configuration.GetSection("RateLimit"));

        return services;
    }

    /// <summary>
    /// Add HTTP clients with configuration
    /// </summary>
    public static IServiceCollection AddHttpClientConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Claude API HTTP client
        services.AddHttpClient<IClaudeService, ClaudeService>(client =>
        {
            var settings = configuration.GetSection("ClaudeApi").Get<ClaudeApiSettings>();
            client.BaseAddress = new Uri(settings?.BaseUrl ?? "https://api.anthropic.com");
            client.Timeout = TimeSpan.FromSeconds(settings?.TimeoutSeconds ?? 60);
            client.DefaultRequestHeaders.Add("User-Agent", "ResumeGenerator/1.0");
        });

        // OpenAI HTTP client
        services.AddHttpClient<IOpenAIService, OpenAIService>(client =>
        {
            var settings = configuration.GetSection("OpenAI").Get<OpenAISettings>();
            client.BaseAddress = new Uri(settings?.BaseUrl ?? "https://api.openai.com/v1");
            client.Timeout = TimeSpan.FromSeconds(settings?.TimeoutSeconds ?? 45);
            client.DefaultRequestHeaders.Add("User-Agent", "ResumeGenerator/1.0");
        });

        // Google Docs HTTP client
        services.AddHttpClient<IGoogleDocsService, GoogleDocsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ResumeGenerator/1.0");
        });

        return services;
    }

    /// <summary>
    /// Add application services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Core services
        services.AddScoped<IResumeGenerationService, ResumeGenerationService>();
        services.AddScoped<IResumeJobService, ResumeJobService>();
        services.AddScoped<IResumeTemplateService, ResumeTemplateService>();

        // AI services
        services.AddScoped<IClaudeService, ClaudeService>();
        services.AddScoped<IOpenAIService, OpenAIService>();
        services.AddScoped<IGoogleDocsService, GoogleDocsService>();

        return services;
    }

    /// <summary>
    /// Add background services
    /// </summary>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<ResumeJobProcessorService>();
        services.AddHostedService<JobCleanupService>();

        return services;
    }

    /// <summary>
    /// Add API configuration
    /// </summary>
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            // Add custom model binding and validation
            options.SuppressAsyncSuffixInActionNames = false;
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        return services;
    }

    /// <summary>
    /// Add CORS configuration
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                var allowedOrigins = configuration.GetSection("CORS:AllowedOrigins").Get<string[]>() 
                    ?? new[] { "http://localhost:3000", "https://localhost:3000" };

                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });

            // Add a more restrictive policy for production
            options.AddPolicy("ProductionPolicy", policy =>
            {
                policy.WithOrigins(configuration.GetSection("CORS:ProductionOrigins").Get<string[]>() ?? Array.Empty<string>())
                      .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                      .WithMethods("GET", "POST", "PUT", "DELETE")
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromHours(1));
            });
        });

        return services;
    }

    /// <summary>
    /// Add health checks configuration
    /// </summary>
    public static IServiceCollection AddHealthChecksConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ResumeGeneratorContext>(
                name: "database",
                tags: new[] { "db", "ready" })
            // Remove the URL health checks and add custom ones
            .AddCheck<ClaudeHealthCheck>(
                name: "claude-api",
                tags: new[] { "external", "ai" })
            .AddCheck<OpenAIHealthCheck>(
                name: "openai-api", 
                tags: new[] { "external", "ai" })
            .AddCheck<CustomHealthCheck>(
                name: "application",
                tags: new[] { "app", "ready" });

        return services;
    }

    /// <summary>
    /// Health check for Claude API
    /// </summary>
    public class ClaudeHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ClaudeHealthCheck> _logger;

        public ClaudeHealthCheck(IServiceScopeFactory scopeFactory, ILogger<ClaudeHealthCheck> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var claudeService = scope.ServiceProvider.GetRequiredService<IClaudeService>();
                
                var isAvailable = await claudeService.IsApiAvailableAsync();
                
                if (isAvailable)
                {
                    return HealthCheckResult.Healthy("Claude API is accessible");
                }
                
                return HealthCheckResult.Unhealthy("Claude API is not accessible");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Claude API health check failed");
                return HealthCheckResult.Unhealthy("Claude API health check failed", ex);
            }
        }
    }

    /// <summary>
    /// Health check for OpenAI API
    /// </summary>
    public class OpenAIHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OpenAIHealthCheck> _logger;

        public OpenAIHealthCheck(IServiceScopeFactory scopeFactory, ILogger<OpenAIHealthCheck> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var openAiService = scope.ServiceProvider.GetRequiredService<IOpenAIService>();
                
                var isAvailable = await openAiService.IsApiAvailableAsync();
                
                if (isAvailable)
                {
                    return HealthCheckResult.Healthy("OpenAI API is accessible");
                }
                
                return HealthCheckResult.Unhealthy("OpenAI API is not accessible");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI API health check failed");
                return HealthCheckResult.Unhealthy("OpenAI API health check failed", ex);
            }
        }
    }

    /// <summary>
    /// Add Swagger/OpenAPI configuration
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Resume Generator API",
                Version = "v1",
                Description = "AI-powered resume generation API using Claude and OpenAI",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Resume Generator Team",
                    Email = "support@resumegenerator.com",
                    Url = new Uri("https://github.com/yourusername/resume-generator-api")
                },
                License = new Microsoft.OpenApi.Models.OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Include XML documentation
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
            foreach (var xmlFile in xmlFiles)
            {
                options.IncludeXmlComments(xmlFile);
            }

            // Add security definition for future authentication
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            // Group endpoints by tags
            options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
        });

        return services;
    }

    /// <summary>
    /// Add memory caching
    /// </summary>
    public static IServiceCollection AddCachingConfiguration(this IServiceCollection services)
    {
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000; // Limit cache size
            options.CompactionPercentage = 0.2; // Remove 20% when limit is reached
        });

        return services;
    }
}

/// <summary>
/// Extension methods for configuring the application pipeline
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configure the application pipeline
    /// </summary>
    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        // Configure development environment
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Resume Generator API v1");
                options.RoutePrefix = string.Empty; // Serve Swagger UI at root
                options.EnableDeepLinking();
                options.EnableValidator();
                options.DisplayRequestDuration();
            });
        }

        // Security headers
        app.UseSecurityHeaders();

        // Custom middleware
        app.UseMiddleware<ResumeGenerator.API.Middleware.ExceptionHandlingMiddleware>();
        app.UseMiddleware<ResumeGenerator.API.Middleware.RequestLoggingMiddleware>();

        // Standard middleware
        app.UseHttpsRedirection();
        app.UseRouting();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // CORS
        var corsPolicy = app.Environment.IsProduction() ? "ProductionPolicy" : "DefaultPolicy";
        app.UseCors(corsPolicy);

        // Authentication & Authorization (for future use)
        // app.UseAuthentication();
        // app.UseAuthorization();

        // Controllers
        app.MapControllers();

        // Health checks
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false
        });

        return app;
    }

    /// <summary>
    /// Add security headers
    /// </summary>
    private static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // Add security headers
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            
            // Remove server header
            context.Response.Headers.Remove("Server");
            
            await next();
        });
    }
}

/// <summary>
/// Custom health check for application-specific logic
/// </summary>
public class CustomHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CustomHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ResumeGeneratorContext>();
            
            // Check if we can query the database
            var templateCount = await dbContext.ResumeTemplates.CountAsync(cancellationToken);
            
            // Check for any critical issues
            var pendingJobsCount = await dbContext.ResumeJobs
                .CountAsync(j => j.Status == Models.Enums.JobStatus.Pending, cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                { "templates_count", templateCount },
                { "pending_jobs", pendingJobsCount },
                { "timestamp", DateTime.UtcNow }
            };

            if (pendingJobsCount > 100) // Example threshold
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                    $"High number of pending jobs: {pendingJobsCount}", data: data);
            }

            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                "Application is healthy", data);
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Application health check failed", ex);
        }
    }
}