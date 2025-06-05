using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Enums;

namespace ResumeGenerator.API.Services.Interfaces;

/// <summary>
/// Service for orchestrating resume generation using AI services
/// </summary>
public interface IResumeGenerationService
{
    /// <summary>
    /// Generate a resume asynchronously
    /// </summary>
    /// <param name="request">Resume generation request</param>
    /// <returns>Generation response with job information</returns>
    Task<ResumeGenerationResponseDto> GenerateResumeAsync(ResumeGenerationRequestDto request);
}

/// <summary>
/// Service for interacting with Claude API
/// </summary>
public interface IClaudeService
{
    /// <summary>
    /// Generate resume content using Claude
    /// </summary>
    /// <param name="template">Resume template content</param>
    /// <param name="personalData">Personal information data</param>
    /// <param name="customInstructions">Additional instructions</param>
    /// <returns>Generated resume content</returns>
    Task<string> GenerateResumeContentAsync(string template, ResumeGenerationRequestDto personalData, string? customInstructions = null);

    /// <summary>
    /// Generate cover letter using Claude
    /// </summary>
    /// <param name="personalData">Personal information and job data</param>
    /// <param name="customInstructions">Additional instructions</param>
    /// <returns>Generated cover letter content</returns>
    Task<string> GenerateCoverLetterAsync(ResumeGenerationRequestDto personalData, string? customInstructions = null);

    /// <summary>
    /// Check if Claude API is available
    /// </summary>
    /// <returns>True if available, false otherwise</returns>
    Task<bool> IsApiAvailableAsync();

    /// <summary>
    /// Get Claude API usage statistics
    /// </summary>
    /// <returns>Usage statistics</returns>
    Task<ApiUsageStatsDto> GetUsageStatsAsync();
}

/// <summary>
/// Service for interacting with OpenAI API
/// </summary>
public interface IOpenAIService
{
    /// <summary>
    /// Review and provide suggestions for generated resume
    /// </summary>
    /// <param name="resumeContent">Generated resume content</param>
    /// <param name="personalData">Original personal data for context</param>
    /// <returns>AI review and suggestions</returns>
    Task<AiReviewDto> ReviewResumeAsync(string resumeContent, ResumeGenerationRequestDto personalData);

    /// <summary>
    /// Review and provide suggestions for generated cover letter
    /// </summary>
    /// <param name="coverLetterContent">Generated cover letter content</param>
    /// <param name="personalData">Original personal data for context</param>
    /// <returns>Cover letter review and suggestions</returns>
    Task<CoverLetterReviewDto> ReviewCoverLetterAsync(string coverLetterContent, ResumeGenerationRequestDto personalData);

    /// <summary>
    /// Analyze how well the resume matches a specific job description
    /// </summary>
    /// <param name="resumeContent">Generated resume content</param>
    /// <param name="personalData">Original personal data including job description</param>
    /// <returns>Job match analysis</returns>
    Task<JobMatchAnalysisDto> AnalyzeJobMatchAsync(string resumeContent, ResumeGenerationRequestDto personalData);

    /// <summary>
    /// Check if OpenAI API is available
    /// </summary>
    /// <returns>True if available, false otherwise</returns>
    Task<bool> IsApiAvailableAsync();

    /// <summary>
    /// Get OpenAI API usage statistics
    /// </summary>
    /// <returns>Usage statistics</returns>
    Task<ApiUsageStatsDto> GetUsageStatsAsync();
}

/// <summary>
/// Service for Google Docs integration
/// </summary>
public interface IGoogleDocsService
{
    /// <summary>
    /// Import template content from Google Docs
    /// </summary>
    /// <param name="documentUrl">Google Docs URL</param>
    /// <param name="credentials">Service account credentials</param>
    /// <returns>Imported content</returns>
    Task<string> ImportTemplateFromGoogleDocsAsync(string documentUrl, string? credentials = null);

    /// <summary>
    /// Export resume content to Google Docs format
    /// </summary>
    /// <param name="content">Resume content</param>
    /// <param name="format">Source format</param>
    /// <returns>Google Docs compatible content</returns>
    Task<string> ConvertToGoogleDocsFormatAsync(string content, OutputFormat format);

    /// <summary>
    /// Validate Google Docs URL
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidGoogleDocsUrl(string url);
}

/// <summary>
/// Service for managing resume templates
/// </summary>
public interface IResumeTemplateService
{
    /// <summary>
    /// Get all available templates
    /// </summary>
    /// <returns>List of templates</returns>
    Task<IEnumerable<ResumeTemplateDto>> GetAllTemplatesAsync();

    /// <summary>
    /// Get template by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Template or null if not found</returns>
    Task<ResumeTemplateDto?> GetTemplateByIdAsync(Guid id);

    /// <summary>
    /// Create a new template
    /// </summary>
    /// <param name="request">Template creation request</param>
    /// <returns>Created template</returns>
    Task<ResumeTemplateDto> CreateTemplateAsync(CreateTemplateRequestDto request);

    /// <summary>
    /// Update an existing template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated template or null if not found</returns>
    Task<ResumeTemplateDto?> UpdateTemplateAsync(Guid id, UpdateTemplateRequestDto request);

    /// <summary>
    /// Delete a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteTemplateAsync(Guid id);

    /// <summary>
    /// Import template from Google Docs
    /// </summary>
    /// <param name="request">Import request</param>
    /// <returns>Created template</returns>
    Task<ResumeTemplateDto> ImportFromGoogleDocsAsync(GoogleDocsImportRequestDto request);

    /// <summary>
    /// Increment template usage count
    /// </summary>
    /// <param name="templateId">Template ID</param>
    Task IncrementUsageCountAsync(Guid templateId);
}

/// <summary>
/// Service for managing resume generation jobs
/// </summary>
public interface IResumeJobService
{
    /// <summary>
    /// Create a new resume generation job
    /// </summary>
    /// <param name="request">Generation request</param>
    /// <returns>Created job ID</returns>
    Task<Guid> CreateJobAsync(ResumeGenerationRequestDto request);

    /// <summary>
    /// Get job status
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Job status or null if not found</returns>
    Task<ResumeJobStatusDto?> GetJobStatusAsync(Guid jobId);

    /// <summary>
    /// Get job result
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Job result or null if not found/completed</returns>
    Task<ResumeContentDto?> GetJobResultAsync(Guid jobId);

    /// <summary>
    /// Get paginated list of jobs
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated job list</returns>
    Task<PagedResultDto<ResumeJobSummaryDto>> GetJobsAsync(JobStatus? status, int pageNumber, int pageSize);

    /// <summary>
    /// Update job status and progress
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="status">New status</param>
    /// <param name="progressPercentage">Progress percentage</param>
    /// <param name="currentStep">Current processing step</param>
    /// <param name="message">Status message</param>
    Task UpdateJobStatusAsync(Guid jobId, JobStatus status, int progressPercentage = 0, ProcessingStep? currentStep = null, string? message = null);

    /// <summary>
    /// Update job with generated content
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="content">Generated resume content</param>
    /// <param name="coverLetterContent">Generated cover letter content (optional)</param>
    /// <param name="aiReview">AI review (optional)</param>
    /// <param name="coverLetterReview">Cover letter review (optional)</param>
    /// <param name="jobMatchAnalysis">Job match analysis (optional)</param>
    /// <param name="metadata">Generation metadata</param>
    Task UpdateJobContentAsync(Guid jobId, string content, string? coverLetterContent, AiReviewDto? aiReview, CoverLetterReviewDto? coverLetterReview, JobMatchAnalysisDto? jobMatchAnalysis, GenerationMetadataDto metadata);

    /// <summary>
    /// Mark job as failed
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorDetails">Detailed error information</param>
    Task MarkJobAsFailedAsync(Guid jobId, string errorMessage, object? errorDetails = null);

    /// <summary>
    /// Cancel a job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>True if cancelled, false if not found or cannot be cancelled</returns>
    Task<bool> CancelJobAsync(Guid jobId);

    /// <summary>
    /// Process pending jobs (background service)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessPendingJobsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Clean up old completed jobs
    /// </summary>
    /// <param name="retentionDays">Number of days to retain</param>
    /// <returns>Number of jobs cleaned up</returns>
    Task<int> CleanupOldJobsAsync(int retentionDays);
}

/// <summary>
/// Service for logging job execution details
/// </summary>
public interface IJobLoggingService
{
    /// <summary>
    /// Log a job step
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="step">Processing step</param>
    /// <param name="message">Log message</param>
    /// <param name="details">Additional details</param>
    /// <param name="durationMs">Step duration in milliseconds</param>
    /// <param name="isError">Whether this is an error log</param>
    Task LogJobStepAsync(Guid jobId, ProcessingStep step, string message, object? details = null, long? durationMs = null, bool isError = false);

    /// <summary>
    /// Get job logs
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>List of job logs</returns>
    Task<IEnumerable<JobLogDto>> GetJobLogsAsync(Guid jobId);
}

/// <summary>
/// API usage statistics DTO
/// </summary>
public class ApiUsageStatsDto
{
    public int RequestsToday { get; set; }
    public int TokensUsedToday { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public int ErrorsToday { get; set; }
    public bool IsHealthy { get; set; }
}

/// <summary>
/// Job log DTO
/// </summary>
public class JobLogDto
{
    public Guid Id { get; set; }
    public ProcessingStep Step { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public long? DurationMs { get; set; }
    public bool IsError { get; set; }
    public object? Details { get; set; }
}