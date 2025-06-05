using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Enums;
using ResumeGenerator.API.Services.Interfaces;
using System.Diagnostics;

namespace ResumeGenerator.API.Services.Implementation;

/// <summary>
/// Main service for orchestrating resume generation using AI services
/// </summary>
public class ResumeGenerationService : IResumeGenerationService
{
    private readonly IResumeJobService _jobService;
    private readonly IResumeTemplateService _templateService;
    private readonly IClaudeService _claudeService;
    private readonly IOpenAIService _openAiService;
    private readonly IGoogleDocsService _googleDocsService;
    private readonly ILogger<ResumeGenerationService> _logger;

    public ResumeGenerationService(
        IResumeJobService jobService,
        IResumeTemplateService templateService,
        IClaudeService claudeService,
        IOpenAIService openAiService,
        IGoogleDocsService googleDocsService,
        ILogger<ResumeGenerationService> logger)
    {
        _jobService = jobService;
        _templateService = templateService;
        _claudeService = claudeService;
        _openAiService = openAiService;
        _googleDocsService = googleDocsService;
        _logger = logger;
    }

    public async Task<ResumeGenerationResponseDto> GenerateResumeAsync(ResumeGenerationRequestDto request)
    {
        // Validate the request
        ValidateRequest(request);

        // Create a job for tracking
        var jobId = await _jobService.CreateJobAsync(request);
        
        _logger.LogInformation("Created resume generation job {JobId} for template {TemplateId}", 
            jobId, request.TemplateId);

        // Start processing asynchronously
        _ = Task.Run(async () => await ProcessResumeGenerationAsync(jobId, request));

        // Return immediate response with job information
        return new ResumeGenerationResponseDto
        {
            JobId = jobId,
            Status = JobStatus.Pending,
            EstimatedCompletion = DateTime.UtcNow.AddMinutes(5), // Estimate 5 minutes
            Message = "Resume generation job has been queued for processing"
        };
    }

    private async Task ProcessResumeGenerationAsync(Guid jobId, ResumeGenerationRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        GenerationMetadataDto metadata = new()
        {
            TemplateId = request.TemplateId,
            GeneratedAt = DateTime.UtcNow
        };

        try
        {
            await _jobService.UpdateJobStatusAsync(jobId, JobStatus.InProgress, 0, ProcessingStep.Validation, "Starting resume generation");

            // Step 1: Load and validate template
            await _jobService.UpdateJobStatusAsync(jobId, JobStatus.InProgress, 10, ProcessingStep.LoadingTemplate, "Loading resume template");
            
            var template = await _templateService.GetTemplateByIdAsync(request.TemplateId);
            if (template == null)
            {
                await _jobService.MarkJobAsFailedAsync(jobId, "Template not found", new { TemplateId = request.TemplateId });
                return;
            }

            // Step 2: Generate content with Claude
            await _jobService.UpdateJobStatusAsync(jobId, JobStatus.InProgress, 30, ProcessingStep.ClaudeGeneration, "Generating resume content with Claude AI");
            
            var generatedContent = await _claudeService.GenerateResumeContentAsync(
                template.Content, 
                request, 
                request.CustomInstructions);

            metadata.ClaudeApiVersion = "claude-sonnet-4-20250514"; // This could be dynamic
            // Token usage would be captured from Claude response in real implementation

            AiReviewDto? aiReview = null;

            // Step 3: Optional AI Review with OpenAI
            if (request.IncludeAiReview)
            {
                await _jobService.UpdateJobStatusAsync(jobId, JobStatus.InProgress, 60, ProcessingStep.OpenAiReview, "Reviewing resume with OpenAI");
                
                try
                {
                    aiReview = await _openAiService.ReviewResumeAsync(generatedContent, request);
                    metadata.OpenAiModel = "gpt-4o"; // This could be dynamic
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenAI review failed for job {JobId}, continuing without review", jobId);
                    // Continue without review rather than failing the entire job
                }
            }

            // Step 4: Generate cover letter if requested
            string? coverLetterContent = null;
            Models.DTOs.CoverLetterReviewDto? coverLetterReview = null;
            if (request.GenerateCoverLetter)
            {
                await _jobService.UpdateJobStatusAsync(jobId, JobStatus.InProgress, 70, ProcessingStep.ClaudeGeneration, "Generating cover letter with Claude AI");
                
                try
                {
                    coverLetterContent = await _claudeService.GenerateCoverLetterAsync(request, request.CustomInstructions);
                    
                    // Review cover letter if AI review is enabled
                    if (request.IncludeAiReview)
                    {
                        await _jobService.UpdateJobStatusAsync(jobId, JobStatus.InProgress, 75, ProcessingStep.OpenAiReview, "Reviewing cover letter with OpenAI");
                        coverLetterReview = await _openAiService.ReviewCoverLetterAsync(coverLetterContent, request);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cover letter generation failed for job {JobId}, continuing without cover letter", jobId);
                    // Continue without cover letter rather than failing the entire job
                }
            }

            // Step 5: Job match analysis if job description provided
            Models.DTOs.JobMatchAnalysisDto? jobMatchAnalysis = null;
            if (request.JobDescription != null && request.IncludeAiReview)
            {
                await _jobService.UpdateJobStatusAsync(jobId, JobStatus.InProgress, 80, ProcessingStep.OpenAiReview, "Analyzing job match with OpenAI");
                
                try
                {
                    jobMatchAnalysis = await _openAiService.AnalyzeJobMatchAsync(generatedContent, request);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Job match analysis failed for job {JobId}, continuing without analysis", jobId);
                    // Continue without analysis rather than failing the entire job
                }
            }

            // Step 6: Format output
            await _jobService.UpdateJobStatusAsync(jobId, JobStatus.InProgress, 85, ProcessingStep.Formatting, "Formatting final output");
            
            var finalContent = await FormatOutput(generatedContent, request.OutputFormat);
            var finalCoverLetter = coverLetterContent != null ? await FormatOutput(coverLetterContent, request.OutputFormat) : null;

            // Step 7: Finalize
            await _jobService.UpdateJobStatusAsync(jobId, JobStatus.InProgress, 95, ProcessingStep.Finalizing, "Finalizing resume");

            stopwatch.Stop();
            metadata.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            // Update job with final content
            await _jobService.UpdateJobContentAsync(jobId, finalContent, finalCoverLetter, aiReview, coverLetterReview, jobMatchAnalysis, metadata);
            await _jobService.UpdateJobStatusAsync(jobId, JobStatus.Completed, 100, null, "Resume generation completed successfully");

            // Increment template usage count
            await _templateService.IncrementUsageCountAsync(request.TemplateId);

            _logger.LogInformation("Resume generation completed successfully for job {JobId} in {ElapsedMs}ms", 
                jobId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Resume generation failed for job {JobId} after {ElapsedMs}ms", 
                jobId, stopwatch.ElapsedMilliseconds);

            await _jobService.MarkJobAsFailedAsync(jobId, "Resume generation failed: " + ex.Message, new 
            { 
                Exception = ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    private async Task<string> FormatOutput(string content, OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Html => content, // Already in HTML format from Claude
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
        // In production, you might use a library like ReverseMarkdown
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
        // Basic HTML to plain text conversion
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
        // Basic HTML to LaTeX conversion
        // This is a simplified version - in production you'd want a more robust converter
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

    private void ValidateRequest(ResumeGenerationRequestDto request)
    {
        if (request.TemplateId == Guid.Empty)
        {
            throw new ArgumentException("Template ID is required");
        }

        if (request.PersonalInfo == null)
        {
            throw new ArgumentException("Personal information is required");
        }

        if (string.IsNullOrWhiteSpace(request.PersonalInfo.FirstName))
        {
            throw new ArgumentException("First name is required");
        }

        if (string.IsNullOrWhiteSpace(request.PersonalInfo.LastName))
        {
            throw new ArgumentException("Last name is required");
        }

        // Additional validation rules can be added here
        if (request.Experience?.Any(e => e.StartDate > DateTime.Now) == true)
        {
            throw new ArgumentException("Experience start dates cannot be in the future");
        }

        if (request.Education?.Any(e => e.StartDate > DateTime.Now || e.EndDate > DateTime.Now) == true)
        {
            throw new ArgumentException("Education dates cannot be in the future");
        }
    }
}