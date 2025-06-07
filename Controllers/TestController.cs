using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ResumeGenerator.API.Configuration;
using ResumeGenerator.API.Services.Interfaces;
using System.Net.Http.Headers;

namespace ResumeGenerator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IClaudeService _claudeService;
    private readonly IOpenAIService _openAiService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;  // Added this
    private readonly OpenAISettings _openAiSettings;
    private readonly ClaudeApiSettings _claudeSettings;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IClaudeService claudeService,
        IOpenAIService openAiService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,  // Added this parameter
        IOptions<OpenAISettings> openAiSettings,
        IOptions<ClaudeApiSettings> claudeSettings,
        ILogger<TestController> logger)
    {
        _claudeService = claudeService;
        _openAiService = openAiService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;  // Added this assignment
        _openAiSettings = openAiSettings.Value;
        _claudeSettings = claudeSettings.Value;
        _logger = logger;
    }

    [HttpGet("claude")]
    public async Task<IActionResult> TestClaude()
    {
        try
        {
            var isAvailable = await _claudeService.IsApiAvailableAsync();
            return Ok(new 
            { 
                service = "Claude",
                available = isAvailable,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API test failed");
            return Ok(new 
            { 
                service = "Claude",
                available = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("openai")]
    public async Task<IActionResult> TestOpenAI()
    {
        try
        {
            var isAvailable = await _openAiService.IsApiAvailableAsync();
            return Ok(new 
            { 
                service = "OpenAI",
                available = isAvailable,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API test failed");
            return Ok(new 
            { 
                service = "OpenAI",
                available = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("openai/direct")]
    public async Task<IActionResult> TestOpenAIDirect()
    {
        try
        {
            // Create a fresh HTTP client to test directly
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_openAiSettings.BaseUrl);
            
            // Set up the request exactly as it should be
            var request = new HttpRequestMessage(HttpMethod.Get, "/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiSettings.ApiKey);
            
            // Only add org header if not empty
            if (!string.IsNullOrEmpty(_openAiSettings.OrganizationId))
            {
                request.Headers.Add("OpenAI-Organization", _openAiSettings.OrganizationId);
            }
            
            _logger.LogInformation("Direct OpenAI test - Request URL: {Url}", httpClient.BaseAddress + "v1/models");
            _logger.LogInformation("Direct OpenAI test - Has Auth header: {HasAuth}", request.Headers.Authorization != null);
            _logger.LogInformation("Direct OpenAI test - API Key length: {Length}", _openAiSettings.ApiKey?.Length ?? 0);
            _logger.LogInformation("Direct OpenAI test - Org ID: {OrgId}", 
                string.IsNullOrEmpty(_openAiSettings.OrganizationId) ? "NOT SET" : _openAiSettings.OrganizationId);
            
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            return Ok(new
            {
                service = "OpenAI Direct Test",
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                statusText = response.ReasonPhrase,
                responseContent = response.IsSuccessStatusCode ? "Success" : content,
                apiKeyConfigured = !string.IsNullOrEmpty(_openAiSettings.ApiKey),
                apiKeyLength = _openAiSettings.ApiKey?.Length ?? 0,
                orgIdConfigured = !string.IsNullOrEmpty(_openAiSettings.OrganizationId),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Direct OpenAI API test failed");
            return Ok(new
            {
                service = "OpenAI Direct Test",
                success = false,
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("google-config")]
    public IActionResult TestGoogleConfiguration()
    {
        try
        {
            var googleSettings = _configuration.GetSection("GoogleDocs").Get<GoogleDocsSettings>();
            
            return Ok(new
            {
                googleDocs = new
                {
                    serviceAccountCredentialsSet = !string.IsNullOrEmpty(googleSettings?.ServiceAccountCredentials),
                    serviceAccountCredentialsLength = googleSettings?.ServiceAccountCredentials?.Length ?? 0,
                    useCredentialsFile = googleSettings?.UseCredentialsFile ?? false,
                    credentialsPath = googleSettings?.UseCredentialsFile == true ? googleSettings?.ServiceAccountCredentials : "JSON_CONTENT",
                    fileExists = googleSettings?.UseCredentialsFile == true && !string.IsNullOrEmpty(googleSettings?.ServiceAccountCredentials) 
                        ? System.IO.File.Exists(googleSettings.ServiceAccountCredentials) : false,
                    templateFolderId = string.IsNullOrEmpty(googleSettings?.TemplateFolderId) ? "NOT SET" : googleSettings.TemplateFolderId,
                    applicationName = googleSettings?.ApplicationName ?? "NOT SET"
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google configuration test failed");
            return Ok(new
            {
                error = ex.Message,
                message = "Failed to read Google configuration",
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("google-file-test")]
    public IActionResult TestGoogleCredentialsFile()
    {
        try
        {
            var googleSettings = _configuration.GetSection("GoogleDocs").Get<GoogleDocsSettings>();
            
            if (googleSettings?.UseCredentialsFile != true || string.IsNullOrEmpty(googleSettings.ServiceAccountCredentials))
            {
                return Ok(new
                {
                    error = "Not configured to use credentials file",
                    useCredentialsFile = googleSettings?.UseCredentialsFile,
                    hasCredentialsPath = !string.IsNullOrEmpty(googleSettings?.ServiceAccountCredentials)
                });
            }

            var filePath = googleSettings.ServiceAccountCredentials;
            var fileExists = System.IO.File.Exists(filePath);
            
            var result = new
            {
                filePath,
                fileExists,
                fileSize = fileExists ? new FileInfo(filePath).Length : 0,
                canRead = false,
                isValidJson = false,
                jsonPreview = "",
                error = ""
            };

            if (fileExists)
            {
                try
                {
                    var content = System.IO.File.ReadAllText(filePath);
                    result = result with 
                    { 
                        canRead = true,
                        jsonPreview = content.Length > 200 ? content.Substring(0, 200) + "..." : content
                    };

                    // Test JSON parsing
                    try
                    {
                        var parsed = System.Text.Json.JsonDocument.Parse(content);
                        result = result with { isValidJson = true };
                    }
                    catch (Exception jsonEx)
                    {
                        result = result with 
                        { 
                            isValidJson = false,
                            error = $"JSON parse error: {jsonEx.Message}"
                        };
                    }
                }
                catch (Exception readEx)
                {
                    result = result with 
                    { 
                        canRead = false,
                        error = $"File read error: {readEx.Message}"
                    };
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                error = ex.Message,
                message = "Failed to test Google credentials file",
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("config")]
    public IActionResult GetConfiguration()
    {
        return Ok(new
        {
            openAI = new
            {
                baseUrl = _openAiSettings.BaseUrl,
                model = _openAiSettings.ReviewModel,
                apiKeySet = !string.IsNullOrEmpty(_openAiSettings.ApiKey),
                apiKeyLength = _openAiSettings.ApiKey?.Length ?? 0,
                apiKeyPrefix = _openAiSettings.ApiKey?.Substring(0, Math.Min(7, _openAiSettings.ApiKey?.Length ?? 0)) + "...",
                organizationId = string.IsNullOrEmpty(_openAiSettings.OrganizationId) ? "NOT SET" : _openAiSettings.OrganizationId,
                timeout = _openAiSettings.TimeoutSeconds
            },
            claude = new
            {
                baseUrl = _claudeSettings.BaseUrl,
                model = _claudeSettings.Model,
                apiKeySet = !string.IsNullOrEmpty(_claudeSettings.ApiKey),
                apiKeyLength = _claudeSettings.ApiKey?.Length ?? 0,
                apiKeyPrefix = _claudeSettings.ApiKey?.Substring(0, Math.Min(10, _claudeSettings.ApiKey?.Length ?? 0)) + "...",
                apiVersion = _claudeSettings.ApiVersion,
                timeout = _claudeSettings.TimeoutSeconds
            }
        });
    }
}