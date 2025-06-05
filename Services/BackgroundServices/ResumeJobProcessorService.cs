using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ResumeGenerator.API.Configuration;
using ResumeGenerator.API.Data;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Entities;
using ResumeGenerator.API.Models.Enums;
using ResumeGenerator.API.Services.Interfaces;
using System.Diagnostics;

namespace ResumeGenerator.API.Services.BackgroundServices;

/// <summary>
/// Background service that processes pending resume generation jobs
/// </summary>
public class ResumeJobProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ApplicationSettings _appSettings;
    private readonly ILogger<ResumeJobProcessorService> _logger;
    private readonly SemaphoreSlim _processingLock;

    public ResumeJobProcessorService(
        IServiceScopeFactory scopeFactory,
        IOptions<ApplicationSettings> appSettings,
        ILogger<ResumeJobProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _appSettings = appSettings.Value;
        _logger = logger;
        _processingLock = new SemaphoreSlim(_appSettings.MaxConcurrentJobs, _appSettings.MaxConcurrentJobs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Resume Job Processor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_appSettings.JobQueuePollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Resume Job Processor Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Resume Job Processor Service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Resume Job Processor Service stopped");
    }

    private async Task ProcessPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ResumeGeneratorContext>();
        
        var pendingJobs = await context.ResumeJobs
            .Where(j => j.Status == JobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .Take(_appSettings.MaxConcurrentJobs)
            .ToListAsync(cancellationToken);

        if (!pendingJobs.Any())
        {
            return;
        }

        _logger.LogInformation("Found {Count} pending jobs to process", pendingJobs.Count);

        var processingTasks = pendingJobs.Select(job => ProcessJobAsync(job.Id, cancellationToken));
        await Task.WhenAll(processingTasks);
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await _processingLock.WaitAsync(cancellationToken);
        
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var services = scope.ServiceProvider;
            
            var context = services.GetRequiredService<ResumeGeneratorContext>();
            var resumeGenerationService = services.GetRequiredService<IResumeGenerationService>();
            var templateService = services.GetRequiredService<IResumeTemplateService>();
            var claudeService = services.GetRequiredService<IClaudeService>();
            var openAiService = services.GetRequiredService<IOpenAIService>();
            var googleDocsService = services.GetRequiredService<IGoogleDocsService>();
            var logger = services.GetRequiredService<ILogger<ResumeJobProcessor>>();

            var processor = new ResumeJobProcessor(
                context, templateService, claudeService, openAiService, googleDocsService, logger);

            await processor.ProcessJobAsync(jobId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
        }
        finally
        {
            _processingLock.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resume Job Processor Service is stopping...");
        await base.StopAsync(cancellationToken);
        _processingLock.Dispose();
    }
}

/// <summary>
/// Individual job processor that handles the resume generation workflow
/// </summary>
public class ResumeJobProcessor
{
    private readonly ResumeGeneratorContext _context;
    private readonly IResumeTemplateService _templateService;
    private readonly IClaudeService _claudeService;
    private readonly IOpenAIService _openAiService;
    private readonly IGoogleDocsService _googleDocsService;
    private readonly ILogger<ResumeJobProcessor> _logger;

    public ResumeJobProcessor(
        ResumeGeneratorContext context,
        IResumeTemplateService templateService,
        IClaudeService claudeService,
        IOpenAIService openAiService,
        IGoogleDocsService googleDocsService,
        ILogger<ResumeJobProcessor> logger)
    {
        _context = context;
        _templateService = templateService;
        _claudeService = claudeService;
        _openAiService = openAiService;
        _googleDocsService = googleDocsService;
        _logger = logger;
    }

    public async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var job = await _context.ResumeJobs
                .Include(j => j.Template)
                .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found", jobId);
                return;
            }

            if (job.Status != JobStatus.Pending)
            {
                _logger.LogInformation("Job {JobId} is no longer pending (status: {Status})", jobId, job.Status);
                return;
            }

            // Check for timeout
            var jobAge = DateTime.UtcNow - job.CreatedAt;
            if (jobAge.TotalMinutes > 30) // 30 minute timeout
            {
                await MarkJobAsTimedOutAsync(job);
                return;
            }

            _logger.LogInformation("Starting processing of job {JobId}", jobId);

            // Update job status to in progress
            await UpdateJobStatusAsync(job, JobStatus.InProgress, 0, ProcessingStep.Validation, "Starting resume generation");

            // Step 1: Validate input data
            if (job.InputData == null)
            {
                await MarkJobAsFailedAsync(job, "Invalid input data");
                return;
            }

            await UpdateJobStatusAsync(job, JobStatus.InProgress, 10, ProcessingStep.LoadingTemplate, "Loading resume template");

            // Step 2: Load template
            if (job.Template == null)
            {
                await MarkJobAsFailedAsync(job, "Template not found");
                return;
            }

            await UpdateJobStatusAsync(job, JobStatus.InProgress, 20, ProcessingStep.ClaudeGeneration, "Generating resume content with Claude AI");

            // Step 3: Generate content with Claude
            var generatedContent = await _claudeService.GenerateResumeContentAsync(
                job.Template.Content,
                job.InputData,
                job.CustomInstructions);

            AiReviewDto? aiReview = null;

            // Step 3: Optional AI Review with OpenAI
            if (job.IncludeAiReview)
            {
                await UpdateJobStatusAsync(job, JobStatus.InProgress, 60, ProcessingStep.OpenAiReview, "Reviewing resume with OpenAI");
                
                try
                {
                    aiReview = await _openAiService.ReviewResumeAsync(generatedContent, job.InputData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenAI review failed for job {JobId}, continuing without review", jobId);
                }
            }

            // Step 4: Generate cover letter if requested
            string? coverLetterContent = null;
            CoverLetterReviewDto? coverLetterReview = null;
            if (job.GenerateCoverLetter)
            {
                await UpdateJobStatusAsync(job, JobStatus.InProgress, 70, ProcessingStep.ClaudeGeneration, "Generating cover letter with Claude AI");
                
                try
                {
                    coverLetterContent = await _claudeService.GenerateCoverLetterAsync(job.InputData, job.CustomInstructions);
                    
                    // Review cover letter if AI review is enabled
                    if (job.IncludeAiReview)
                    {
                        await UpdateJobStatusAsync(job, JobStatus.InProgress, 75, ProcessingStep.OpenAiReview, "Reviewing cover letter with OpenAI");
                        coverLetterReview = await _openAiService.ReviewCoverLetterAsync(coverLetterContent, job.InputData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cover letter generation failed for job {JobId}, continuing without cover letter", jobId);
                }
            }

            // Step 5: Job match analysis if job description provided
            JobMatchAnalysisDto? jobMatchAnalysis = null;
            if (job.JobDescription != null && job.IncludeAiReview)
            {
                await UpdateJobStatusAsync(job, JobStatus.InProgress, 80, ProcessingStep.OpenAiReview, "Analyzing job match with OpenAI");
                
                try
                {
                    jobMatchAnalysis = await _openAiService.AnalyzeJobMatchAsync(generatedContent, job.InputData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Job match analysis failed for job {JobId}, continuing without analysis", jobId);
                }
            }

            await UpdateJobStatusAsync(job, JobStatus.InProgress, 85, ProcessingStep.Formatting, "Formatting final output");

            // Step 4: Optional AI Review
            Models.DTOs.AiReviewDto? aiReview = null;
            if (job.IncludeAiReview)
            {
                await UpdateJobStatusAsync(job, JobStatus.InProgress, 70, ProcessingStep.OpenAiReview, "Reviewing resume with OpenAI");
                
                try
                {
                    aiReview = await _openAiService.ReviewResumeAsync(generatedContent, job.InputData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenAI review failed for job {JobId}, continuing without review", jobId);
                }
            }

            await UpdateJobStatusAsync(job, JobStatus.InProgress, 85, ProcessingStep.Formatting, "Formatting final output");

            // Step 6: Format output based on requested format
            var finalContent = await FormatOutputAsync(generatedContent, job.OutputFormat);
            var finalCoverLetter = coverLetterContent != null ? await FormatOutputAsync(coverLetterContent, job.OutputFormat) : null;

            await UpdateJobStatusAsync(job, JobStatus.InProgress, 95, ProcessingStep.Finalizing, "Finalizing resume");

            // Step 7: Save final content
            stopwatch.Stop();
            
            job.GeneratedContent = finalContent;
            job.CoverLetterContent = finalCoverLetter;
            job.AiReview = aiReview;
            job.CoverLetterReview = coverLetterReview;
            job.JobMatchAnalysis = jobMatchAnalysis;
            job.Status = JobStatus.Completed;
            job.ProgressPercentage = 100;
            job.CompletedAt = DateTime.UtcNow;
            job.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            job.ClaudeApiVersion = "claude-sonnet-4-20250514"; // This would come from actual API response
            
            if (aiReview != null)
            {
                job.OpenAiModel = "gpt-4o"; // This would come from actual API response
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Increment template usage count
            await _templateService.IncrementUsageCountAsync(job.TemplateId);

            _logger.LogInformation("Successfully completed job {JobId} in {ElapsedMs}ms", 
                jobId, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Job {JobId} processing was cancelled", jobId);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to process job {JobId} after {ElapsedMs}ms", 
                jobId, stopwatch.ElapsedMilliseconds);

            var job = await _context.ResumeJobs.FindAsync(jobId);
            if (job != null)
            {
                await MarkJobAsFailedAsync(job, $"Processing failed: {ex.Message}");
            }
        }
    }

    private async Task<string> FormatOutputAsync(string content, OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Html => content,
            OutputFormat.GoogleDocs => await _googleDocsService.ConvertToGoogleDocsFormatAsync(content, OutputFormat.Html),
            OutputFormat.Markdown => ConvertHtmlToMarkdown(content),
            OutputFormat.PlainText => ConvertHtmlToPlainText(content),
            OutputFormat.LaTeX => ConvertHtmlToLaTeX(content),
            _ => content
        };
    }

    private string ConvertHtmlToMarkdown(string htmlContent)
    {
        // Basic HTML to Markdown conversion
        return htmlContent
            .Replace("<h1>", "# ").Replace("</h1>", "\n\n")
            .Replace("<h2>", "## ").Replace("</h2>", "\n\n")
            .Replace("<h3>", "### ").Replace("</h3>", "\n\n")
            .Replace("<p>", "").Replace("</p>", "\n\n")
            .Replace("<strong>", "**").Replace("</strong>", "**")
            .Replace("<em>", "*").Replace("</em>", "*")
            .Replace("<ul>", "").Replace("</ul>", "\n")
            .Replace("<li>", "- ").Replace("</li>", "\n")
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n");
    }

    private string ConvertHtmlToPlainText(string htmlContent)
    {
        return System.Text.RegularExpressions.Regex.Replace(htmlContent, "<.*?>", string.Empty)
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Trim();
    }

    private string ConvertHtmlToLaTeX(string htmlContent)
    {
        var latex = htmlContent
            .Replace("<h1>", "\\section{").Replace("</h1>", "}")
            .Replace("<h2>", "\\subsection{").Replace("</h2>", "}")
            .Replace("<h3>", "\\subsubsection{").Replace("</h3>", "}")
            .Replace("<p>", "").Replace("</p>", "\n\n")
            .Replace("<strong>", "\\textbf{").Replace("</strong>", "}")
            .Replace("<em>", "\\textit{").Replace("</em>", "}")
            .Replace("<ul>", "\\begin{itemize}").Replace("</ul>", "\\end{itemize}")
            .Replace("<li>", "\\item ").Replace("</li>", "")
            .Replace("<br>", "\\\\")
            .Replace("<br/>", "\\\\")
            .Replace("<br />", "\\\\");

        return $@"\documentclass{{article}}
\usepackage[utf8]{{inputenc}}
\usepackage{{geometry}}
\geometry{{margin=1in}}

\begin{{document}}

{latex}

\end{{document}}";
    }

    private async Task UpdateJobStatusAsync(ResumeJob job, JobStatus status, int progress, ProcessingStep? step, string message)
    {
        job.Status = status;
        job.ProgressPercentage = progress;
        job.CurrentStep = step;
        job.CurrentStepDescription = message;
        job.UpdatedAt = DateTime.UtcNow;

        if (status == JobStatus.InProgress && job.StartedAt == null)
        {
            job.StartedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private async Task MarkJobAsFailedAsync(ResumeJob job, string errorMessage)
    {
        job.Status = JobStatus.Failed;
        job.ErrorMessage = errorMessage;
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;

        if (job.StartedAt.HasValue)
        {
            job.ProcessingTimeMs = (long)(job.CompletedAt.Value - job.StartedAt.Value).TotalMilliseconds;
        }

        await _context.SaveChangesAsync();
    }

    private async Task MarkJobAsTimedOutAsync(ResumeJob job)
    {
        job.Status = JobStatus.Failed;
        job.ErrorMessage = "Job timed out";
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;

        if (job.StartedAt.HasValue)
        {
            job.ProcessingTimeMs = (long)(job.CompletedAt.Value - job.StartedAt.Value).TotalMilliseconds;
        }

        await _context.SaveChangesAsync();
        _logger.LogWarning("Job {JobId} timed out after {Age} minutes", job.Id, (DateTime.UtcNow - job.CreatedAt).TotalMinutes);
    }
}

/// <summary>
/// Background service for cleaning up old completed jobs
/// </summary>
public class JobCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ApplicationSettings _appSettings;
    private readonly ILogger<JobCleanupService> _logger;

    public JobCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<ApplicationSettings> appSettings,
        ILogger<JobCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldJobsAsync(stoppingToken);
                
                // Run cleanup daily
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Job Cleanup Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Job Cleanup Service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Job Cleanup Service stopped");
    }

    private async Task CleanupOldJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ResumeGeneratorContext>();
        
        var completedCutoff = DateTime.UtcNow.AddDays(-_appSettings.JobRetentionDays);
        var failedCutoff = DateTime.UtcNow.AddDays(-_appSettings.FailedJobRetentionDays);

        // Clean up old completed jobs
        var oldCompletedJobs = await context.ResumeJobs
            .Where(j => j.Status == JobStatus.Completed && j.CompletedAt < completedCutoff)
            .ToListAsync(cancellationToken);

        // Clean up old failed jobs
        var oldFailedJobs = await context.ResumeJobs
            .Where(j => (j.Status == JobStatus.Failed || j.Status == JobStatus.Cancelled) && j.CompletedAt < failedCutoff)
            .ToListAsync(cancellationToken);

        var totalCleaned = oldCompletedJobs.Count + oldFailedJobs.Count;

        if (totalCleaned > 0)
        {
            context.ResumeJobs.RemoveRange(oldCompletedJobs);
            context.ResumeJobs.RemoveRange(oldFailedJobs);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {CompletedCount} completed jobs and {FailedCount} failed jobs", 
                oldCompletedJobs.Count, oldFailedJobs.Count);
        }
        else
        {
            _logger.LogDebug("No old jobs to clean up");
        }
    }
}