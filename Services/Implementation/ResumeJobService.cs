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