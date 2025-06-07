namespace ResumeGenerator.API.Configuration;

/// <summary>
/// Configuration settings for Claude API
/// </summary>
public class ClaudeApiSettings
{
    /// <summary>
    /// Claude API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Claude API base URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    /// <summary>
    /// Claude model to use for generation
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-20250514";

    /// <summary>
    /// API version
    /// </summary>
    public string ApiVersion { get; set; } = "2023-06-01";

    /// <summary>
    /// Maximum tokens for generation
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature for generation (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}

/// <summary>
/// Configuration settings for OpenAI API
/// </summary>
public class OpenAISettings
{
    /// <summary>
    /// OpenAI API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI API base URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// OpenAI model to use for review
    /// </summary>
    public string ReviewModel { get; set; } = "gpt-4o";

    /// <summary>
    /// Maximum tokens for review
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Temperature for review (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 45;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Organization ID (optional)
    /// </summary>
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Configuration settings for Google Docs integration
/// </summary>
public class GoogleDocsSettings
{
    /// <summary>
    /// Google service account key file path or JSON content
    /// </summary>
    public string ServiceAccountCredentials { get; set; } = string.Empty;

    /// <summary>
    /// Application name for Google API
    /// </summary>
    public string ApplicationName { get; set; } = "Resume Generator API";

    /// <summary>
    /// Default timeout for Google API requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for Google API calls
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Google Drive folder ID containing resume templates
    /// </summary>
    public string TemplateFolderId { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use file path or JSON content for credentials
    /// </summary>
    public bool UseCredentialsFile { get; set; } = true;
}

/// <summary>
/// General application settings
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Maximum concurrent job processing
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 5;

    /// <summary>
    /// Job timeout in minutes
    /// </summary>
    public int JobTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// How long to retain completed jobs (in days)
    /// </summary>
    public int JobRetentionDays { get; set; } = 30;

    /// <summary>
    /// How long to retain failed jobs (in days)
    /// </summary>
    public int FailedJobRetentionDays { get; set; } = 7;

    /// <summary>
    /// Enable detailed logging for debugging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Enable job queue processing
    /// </summary>
    public bool EnableJobQueue { get; set; } = true;

    /// <summary>
    /// Job queue polling interval in seconds
    /// </summary>
    public int JobQueuePollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum file size for template uploads (in MB)
    /// </summary>
    public int MaxTemplateSizeMb { get; set; } = 10;

    /// <summary>
    /// Allowed template file extensions
    /// </summary>
    public List<string> AllowedTemplateExtensions { get; set; } = new() { ".html", ".md", ".txt" };
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests per minute per IP
    /// </summary>
    public int RequestsPerMinute { get; set; } = 10;

    /// <summary>
    /// Maximum concurrent jobs per IP
    /// </summary>
    public int MaxConcurrentJobsPerIp { get; set; } = 3;

    /// <summary>
    /// Whitelist of IP addresses exempt from rate limiting
    /// </summary>
    public List<string> WhitelistedIps { get; set; } = new();
}