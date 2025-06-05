using Microsoft.EntityFrameworkCore;
using ResumeGenerator.API.Data;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Entities;
using ResumeGenerator.API.Models.Enums;
using ResumeGenerator.API.Services.Interfaces;

namespace ResumeGenerator.API.Services.Implementation;

/// <summary>
/// Service for managing resume generation jobs
/// </summary>
public class ResumeJobService : IResumeJobService
{
    private readonly ResumeGeneratorContext _context;
    private readonly ILogger<ResumeJobService> _logger;

    public ResumeJobService(ResumeGeneratorContext context, ILogger<ResumeJobService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> CreateJobAsync(ResumeGenerationRequestDto request)
    {
        var job = new ResumeJob
        {
            TemplateId = request.TemplateId,
            Status = JobStatus.Pending,
            InputData = request,
            OutputFormat = request.OutputFormat,
            IncludeAiReview = request.IncludeAiReview,
            GenerateCoverLetter = request.GenerateCoverLetter,
            JobDescription = request.JobDescription,
            CustomInstructions = request.CustomInstructions,
            EstimatedCompletion = DateTime.UtcNow.AddMinutes(request.GenerateCoverLetter ? 8 : 5) // Longer estimate for cover letter
        };

        _context.ResumeJobs.Add(job);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created resume generation job {JobId}", job.Id);
        return job.Id;
    }

    public async Task<ResumeJobStatusDto?> GetJobStatusAsync(Guid jobId)
    {
        var job = await _context.ResumeJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return null;

        return new ResumeJobStatusDto
        {
            JobId = job.Id,
            Status = job.Status,
            ProgressPercentage = job.ProgressPercentage,
            CurrentStep = job.CurrentStepDescription ?? job.CurrentStep?.ToString() ?? "Pending",
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            EstimatedCompletion = job.EstimatedCompletion,
            ErrorMessage = job.ErrorMessage
        };
    }

    public async Task<ResumeContentDto?> GetJobResultAsync(Guid jobId)
    {
        var job = await _context.ResumeJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId && j.Status == JobStatus.Completed);

        if (job?.GeneratedContent == null)
            return null;

        return new ResumeContentDto
        {
            GeneratedContent = job.GeneratedContent,
            CoverLetterContent = job.CoverLetterContent,
            Format = job.OutputFormat,
            AiReview = job.AiReview,
            CoverLetterReview = job.CoverLetterReview,
            JobMatchAnalysis = job.JobMatchAnalysis,
            Metadata = new GenerationMetadataDto
            {
                TemplateId = job.TemplateId,
                GeneratedAt = job.CompletedAt ?? job.UpdatedAt,
                ProcessingTimeMs = job.ProcessingTimeMs ?? 0,
                ClaudeApiVersion = job.ClaudeApiVersion ?? "unknown",
                OpenAiModel = job.OpenAiModel,
                TokenUsage = new TokenUsageDto
                {
                    ClaudeInputTokens = job.ClaudeInputTokens ?? 0,
                    ClaudeOutputTokens = job.ClaudeOutputTokens ?? 0,
                    OpenAiInputTokens = job.OpenAiInputTokens,
                    OpenAiOutputTokens = job.OpenAiOutputTokens
                }
            }
        };
    }

    public async Task<PagedResultDto<ResumeJobSummaryDto>> GetJobsAsync(JobStatus? status, int pageNumber, int pageSize)
    {
        var query = _context.ResumeJobs
            .Include(j => j.Template)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new ResumeJobSummaryDto
            {
                JobId = j.Id,
                TemplateName = j.Template.Name,
                PersonName = j.InputData != null ? 
                    $"{j.InputData.PersonalInfo.FirstName} {j.InputData.PersonalInfo.LastName}" : 
                    "Unknown",
                Status = j.Status,
                CreatedAt = j.CreatedAt,
                CompletedAt = j.CompletedAt,
                ProcessingTimeMs = j.ProcessingTimeMs
            })
            .ToListAsync();

        return new PagedResultDto<ResumeJobSummaryDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber < totalPages,
            Items = jobs
        };
    }

    public async Task UpdateJobStatusAsync(Guid jobId, JobStatus status, int progressPercentage = 0, ProcessingStep? currentStep = null, string? message = null)
    {
        var job = await _context.ResumeJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Attempted to update non-existent job {JobId}", jobId);
            return;
        }

        job.Status = status;
        job.ProgressPercentage = Math.Clamp(progressPercentage, 0, 100);
        job.CurrentStep = currentStep;
        job.CurrentStepDescription = message;

        if (status == JobStatus.InProgress && job.StartedAt == null)
        {
            job.StartedAt = DateTime.UtcNow;
        }
        else if (status == JobStatus.Completed || status == JobStatus.Failed || status == JobStatus.Cancelled)
        {
            job.CompletedAt = DateTime.UtcNow;
            if (job.StartedAt.HasValue)
            {
                job.ProcessingTimeMs = (long)(job.CompletedAt.Value - job.StartedAt.Value).TotalMilliseconds;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogDebug("Updated job {JobId} status to {Status} ({Progress}%)", jobId, status, progressPercentage);
    }

    public async Task UpdateJobContentAsync(Guid jobId, string content, string? coverLetterContent, AiReviewDto? aiReview, CoverLetterReviewDto? coverLetterReview, JobMatchAnalysisDto? jobMatchAnalysis, GenerationMetadataDto metadata)
    {
        var job = await _context.ResumeJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Attempted to update content for non-existent job {JobId}", jobId);
            return;
        }

        job.GeneratedContent = content;
        job.CoverLetterContent = coverLetterContent;
        job.AiReview = aiReview;
        job.CoverLetterReview = coverLetterReview;
        job.JobMatchAnalysis = jobMatchAnalysis;
        job.ClaudeApiVersion = metadata.ClaudeApiVersion;
        job.OpenAiModel = metadata.OpenAiModel;
        job.ClaudeInputTokens = metadata.TokenUsage.ClaudeInputTokens;
        job.ClaudeOutputTokens = metadata.TokenUsage.ClaudeOutputTokens;
        job.OpenAiInputTokens = metadata.TokenUsage.OpenAiInputTokens;
        job.OpenAiOutputTokens = metadata.TokenUsage.OpenAiOutputTokens;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated content for job {JobId}", jobId);
    }

    public async Task MarkJobAsFailedAsync(Guid jobId, string errorMessage, object? errorDetails = null)
    {
        var job = await _context.ResumeJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Attempted to mark non-existent job {JobId} as failed", jobId);
            return;
        }

        job.Status = JobStatus.Failed;
        job.ErrorMessage = errorMessage;
        job.ErrorDetailsJson = errorDetails != null ? 
            System.Text.Json.JsonSerializer.Serialize(errorDetails) : null;
        job.CompletedAt = DateTime.UtcNow;

        if (job.StartedAt.HasValue)
        {
            job.ProcessingTimeMs = (long)(job.CompletedAt.Value - job.StartedAt.Value).TotalMilliseconds;
        }

        await _context.SaveChangesAsync();
        _logger.LogError("Marked job {JobId} as failed: {ErrorMessage}", jobId, errorMessage);
    }

    public async Task<bool> CancelJobAsync(Guid jobId)
    {
        var job = await _context.ResumeJobs.FindAsync(jobId);
        if (job == null)
        {
            return false;
        }

        if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed || job.Status == JobStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel job in {job.Status} status");
        }

        job.Status = JobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;

        if (job.StartedAt.HasValue)
        {
            job.ProcessingTimeMs = (long)(job.CompletedAt.Value - job.StartedAt.Value).TotalMilliseconds;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Cancelled job {JobId}", jobId);

        return true;
    }

    public async Task ProcessPendingJobsAsync(CancellationToken cancellationToken)
    {
        // This would be implemented as a background service
        // For now, it's a placeholder for the background job processing logic
        _logger.LogInformation("Processing pending jobs (placeholder implementation)");
        await Task.CompletedTask;
    }

    public async Task<int> CleanupOldJobsAsync(int retentionDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        
        var oldJobs = await _context.ResumeJobs
            .Where(j => j.CompletedAt < cutoffDate && 
                       (j.Status == JobStatus.Completed || j.Status == JobStatus.Failed || j.Status == JobStatus.Cancelled))
            .ToListAsync();

        _context.ResumeJobs.RemoveRange(oldJobs);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} old jobs older than {Days} days", oldJobs.Count, retentionDays);
        return oldJobs.Count;
    }
}

/// <summary>
/// Service for managing resume templates
/// </summary>
public class ResumeTemplateService : IResumeTemplateService
{
    private readonly ResumeGeneratorContext _context;
    private readonly IGoogleDocsService _googleDocsService;
    private readonly ILogger<ResumeTemplateService> _logger;

    public ResumeTemplateService(
        ResumeGeneratorContext context,
        IGoogleDocsService googleDocsService,
        ILogger<ResumeTemplateService> logger)
    {
        _context = context;
        _googleDocsService = googleDocsService;
        _logger = logger;
    }

    public async Task<IEnumerable<ResumeTemplateDto>> GetAllTemplatesAsync()
    {
        var templates = await _context.ResumeTemplates
            .Where(t => t.IsActive)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return templates.Select(MapToDto);
    }

    public async Task<ResumeTemplateDto?> GetTemplateByIdAsync(Guid id)
    {
        var template = await _context.ResumeTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        return template != null ? MapToDto(template) : null;
    }

    public async Task<ResumeTemplateDto> CreateTemplateAsync(CreateTemplateRequestDto request)
    {
        var template = new ResumeTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Content = request.Content,
            Format = request.Format,
            Tags = request.Tags,
            IsPublic = request.IsPublic
        };

        _context.ResumeTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created template {TemplateId} with name '{Name}'", template.Id, template.Name);
        return MapToDto(template);
    }

    public async Task<ResumeTemplateDto?> UpdateTemplateAsync(Guid id, UpdateTemplateRequestDto request)
    {
        var template = await _context.ResumeTemplates.FindAsync(id);
        if (template == null || !template.IsActive)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.Name))
            template.Name = request.Name;
        
        if (request.Description != null)
            template.Description = request.Description;
        
        if (!string.IsNullOrEmpty(request.Content))
            template.Content = request.Content;
        
        if (request.Format.HasValue)
            template.Format = request.Format.Value;
        
        if (request.Tags != null)
            template.Tags = request.Tags;
        
        if (request.IsPublic.HasValue)
            template.IsPublic = request.IsPublic.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated template {TemplateId}", id);
        return MapToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        var template = await _context.ResumeTemplates.FindAsync(id);
        if (template == null || !template.IsActive)
        {
            return false;
        }

        // Soft delete
        template.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted template {TemplateId}", id);
        return true;
    }

    public async Task<ResumeTemplateDto> ImportFromGoogleDocsAsync(GoogleDocsImportRequestDto request)
    {
        if (!_googleDocsService.IsValidGoogleDocsUrl(request.DocumentUrl))
        {
            throw new ArgumentException("Invalid Google Docs URL");
        }

        var content = await _googleDocsService.ImportTemplateFromGoogleDocsAsync(
            request.DocumentUrl, 
            request.ServiceAccountCredentials);

        var createRequest = new CreateTemplateRequestDto
        {
            Name = request.TemplateName,
            Description = request.Description,
            Content = content,
            Format = TemplateFormat.GoogleDocs,
            Tags = request.Tags,
            IsPublic = request.IsPublic
        };

        return await CreateTemplateAsync(createRequest);
    }

    public async Task IncrementUsageCountAsync(Guid templateId)
    {
        var template = await _context.ResumeTemplates.FindAsync(templateId);
        if (template != null)
        {
            template.UsageCount++;
            await _context.SaveChangesAsync();
        }
    }

    private static ResumeTemplateDto MapToDto(ResumeTemplate template)
    {
        return new ResumeTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Content = template.Content,
            Format = template.Format,
            Tags = template.Tags,
            IsPublic = template.IsPublic,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            UsageCount = template.UsageCount
        };
    }
}

/// <summary>
/// Basic Google Docs service implementation
/// </summary>
public class GoogleDocsService : IGoogleDocsService
{
    private readonly ILogger<GoogleDocsService> _logger;

    public GoogleDocsService(ILogger<GoogleDocsService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ImportTemplateFromGoogleDocsAsync(string documentUrl, string? credentials = null)
    {
        // This is a placeholder implementation
        // In production, you would use Google.Apis.Docs.v1 to import content
        _logger.LogInformation("Importing template from Google Docs: {Url}", documentUrl);
        
        await Task.Delay(1000); // Simulate API call
        
        return @"<html>
<head><title>Imported Google Docs Template</title></head>
<body>
    <h1>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}}</h1>
    <p>{{PersonalInfo.Email}} | {{PersonalInfo.Phone}}</p>
    <p>This template was imported from Google Docs</p>
</body>
</html>";
    }

    public async Task<string> ConvertToGoogleDocsFormatAsync(string content, OutputFormat format)
    {
        _logger.LogInformation("Converting content to Google Docs format from {Format}", format);
        
        await Task.Delay(500); // Simulate conversion
        
        // Basic HTML to Google Docs conversion
        return content; // In production, this would be a proper conversion
    }

    public bool IsValidGoogleDocsUrl(string url)
    {
        return !string.IsNullOrEmpty(url) && 
               (url.Contains("docs.google.com") || url.Contains("drive.google.com"));
    }
}