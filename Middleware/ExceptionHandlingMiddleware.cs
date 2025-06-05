using System.Net;
using System.Text.Json;

namespace ResumeGenerator.API.Middleware;

/// <summary>
/// Middleware for global exception handling
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "Required parameter is missing"),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "Request timed out"),
            NotImplementedException => (HttpStatusCode.NotImplemented, "Feature not implemented"),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = new
            {
                message,
                type = exception.GetType().Name,
                timestamp = DateTime.UtcNow,
                traceId = context.TraceIdentifier
            }
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Middleware for request/response logging
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();

        // Log request
        _logger.LogInformation("Request {RequestId} started: {Method} {Path}",
            requestId, context.Request.Method, context.Request.Path);

        // Add request ID to response headers
        context.Response.Headers.Add("X-Request-ID", requestId);

        try
        {
            await _next(context);
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("Request {RequestId} completed: {Method} {Path} {StatusCode} in {Duration}ms",
                requestId, 
                context.Request.Method, 
                context.Request.Path, 
                context.Response.StatusCode, 
                duration.TotalMilliseconds);
        }
    }
}

/// <summary>
/// Middleware for API rate limiting
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly Dictionary<string, ClientRequestInfo> _clients = new();
    private readonly object _lock = new();

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        
        if (IsRateLimited(clientIp))
        {
            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                error = new
                {
                    message = "Rate limit exceeded. Please try again later.",
                    type = "RateLimitExceeded",
                    timestamp = DateTime.UtcNow
                }
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
            return;
        }

        await _next(context);
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (in case behind a proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsRateLimited(string clientIp)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            
            if (!_clients.TryGetValue(clientIp, out var clientInfo))
            {
                clientInfo = new ClientRequestInfo();
                _clients[clientIp] = clientInfo;
            }

            // Clean up old requests (older than 1 minute)
            clientInfo.RequestTimes.RemoveAll(time => now - time > TimeSpan.FromMinutes(1));

            // Check if rate limit exceeded (10 requests per minute)
            if (clientInfo.RequestTimes.Count >= 10)
            {
                _logger.LogWarning("Rate limit exceeded for client IP: {ClientIp}", clientIp);
                return true;
            }

            // Add current request
            clientInfo.RequestTimes.Add(now);
            return false;
        }
    }

    private class ClientRequestInfo
    {
        public List<DateTime> RequestTimes { get; } = new();
    }
}

/// <summary>
/// Middleware for CORS headers (if not using built-in CORS)
/// </summary>
public class CorsMiddleware
{
    private readonly RequestDelegate _next;

    public CorsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 200;
            return;
        }

        await _next(context);
    }
}