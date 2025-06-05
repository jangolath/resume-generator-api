using Microsoft.AspNetCore.Mvc;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Enums;
using ResumeGenerator.API.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace ResumeGenerator.API.Controllers;

/// <summary>
/// Controller for resume generation operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ResumeController : ControllerBase
{
    private readonly IResumeGenerationService _resumeGenerationService;
    private readonly IResumeJobService _resumeJobService;
    private readonly ILogger<ResumeController> _logger;

    public ResumeController(
        IResumeGenerationService resumeGenerationService,
        IResumeJobService resumeJobService,
        ILogger<ResumeController> logger)
    {
        _resumeGenerationService = resumeGenerationService;
        _resumeJobService = resumeJobService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a resume using AI services
    /// </summary>
    /// <param name="request">Resume generation request</param>
    /// <returns>Generated resume response</returns>
    /// <response code="200">Resume generated successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(ResumeGenerationResponseDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ResumeGenerationResponseDto>> GenerateResume(
        [FromBody, Required] ResumeGenerationRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting resume generation for template: {TemplateId}", request.TemplateId);
            
            var result = await _resumeGenerationService.GenerateResumeAsync(request);
            
            _logger.LogInformation("Resume generation completed successfully with job ID: {JobId}", result.JobId);
            
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters for resume generation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating resume");
            return StatusCode(500, new { error = "An error occurred while generating the resume" });
        }
    }

    /// <summary>
    /// Get the status of a resume generation job
    /// </summary>
    /// <param name="jobId">The job ID to check</param>
    /// <returns>Job status information</returns>
    /// <response code="200">Job status retrieved successfully</response>
    /// <response code="404">Job not found</response>
    [HttpGet("job/{jobId:guid}/status")]
    [ProducesResponseType(typeof(ResumeJobStatusDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ResumeJobStatusDto>> GetJobStatus(Guid jobId)
    {
        try
        {
            var status = await _resumeJobService.GetJobStatusAsync(jobId);
            
            if (status == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status for {JobId}", jobId);
            return StatusCode(500, new { error = "An error occurred while retrieving job status" });
        }
    }

    /// <summary>
    /// Get the result of a completed resume generation job
    /// </summary>
    /// <param name="jobId">The job ID to retrieve results for</param>
    /// <returns>Generated resume content</returns>
    /// <response code="200">Resume content retrieved successfully</response>
    /// <response code="404">Job not found or not completed</response>
    [HttpGet("job/{jobId:guid}/result")]
    [ProducesResponseType(typeof(ResumeContentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ResumeContentDto>> GetJobResult(Guid jobId)
    {
        try
        {
            var result = await _resumeJobService.GetJobResultAsync(jobId);
            
            if (result == null)
            {
                return NotFound(new { error = "Job not found or not completed" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job result for {JobId}", jobId);
            return StatusCode(500, new { error = "An error occurred while retrieving job result" });
        }
    }

    /// <summary>
    /// Get all resume generation jobs for monitoring
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of jobs</returns>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(PagedResultDto<ResumeJobSummaryDto>), 200)]
    public async Task<ActionResult<PagedResultDto<ResumeJobSummaryDto>>> GetJobs(
        [FromQuery] JobStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            // Validate pagination parameters
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Min(100, Math.Max(1, pageSize));

            var jobs = await _resumeJobService.GetJobsAsync(status, pageNumber, pageSize);
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs list");
            return StatusCode(500, new { error = "An error occurred while retrieving jobs" });
        }
    }

    /// <summary>
    /// Cancel a pending or in-progress job
    /// </summary>
    /// <param name="jobId">The job ID to cancel</param>
    /// <returns>Cancellation confirmation</returns>
    [HttpPost("job/{jobId:guid}/cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CancelJob(Guid jobId)
    {
        try
        {
            var success = await _resumeJobService.CancelJobAsync(jobId);
            
            if (!success)
            {
                return NotFound(new { error = "Job not found or cannot be cancelled" });
            }

            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
            return StatusCode(500, new { error = "An error occurred while cancelling the job" });
        }
    }
}