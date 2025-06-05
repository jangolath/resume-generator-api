using Microsoft.AspNetCore.Mvc;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace ResumeGenerator.API.Controllers;

/// <summary>
/// Controller for managing resume templates
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TemplateController : ControllerBase
{
    private readonly IResumeTemplateService _templateService;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(
        IResumeTemplateService templateService,
        ILogger<TemplateController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available resume templates
    /// </summary>
    /// <returns>List of available templates</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResumeTemplateDto>), 200)]
    public async Task<ActionResult<IEnumerable<ResumeTemplateDto>>> GetTemplates()
    {
        try
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates");
            return StatusCode(500, new { error = "An error occurred while retrieving templates" });
        }
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Template details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResumeTemplateDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ResumeTemplateDto>> GetTemplate(Guid id)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(id);
            
            if (template == null)
            {
                return NotFound(new { error = "Template not found" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the template" });
        }
    }

    /// <summary>
    /// Create a new resume template
    /// </summary>
    /// <param name="request">Template creation request</param>
    /// <returns>Created template</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ResumeTemplateDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<ActionResult<ResumeTemplateDto>> CreateTemplate(
        [FromBody, Required] CreateTemplateRequestDto request)
    {
        try
        {
            var template = await _templateService.CreateTemplateAsync(request);
            
            return CreatedAtAction(
                nameof(GetTemplate), 
                new { id = template.Id }, 
                template);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid template creation request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, new { error = "An error occurred while creating the template" });
        }
    }

    /// <summary>
    /// Update an existing template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Template update request</param>
    /// <returns>Updated template</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ResumeTemplateDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<ActionResult<ResumeTemplateDto>> UpdateTemplate(
        Guid id,
        [FromBody, Required] UpdateTemplateRequestDto request)
    {
        try
        {
            var template = await _templateService.UpdateTemplateAsync(id, request);
            
            if (template == null)
            {
                return NotFound(new { error = "Template not found" });
            }

            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid template update request for {TemplateId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the template" });
        }
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        try
        {
            var success = await _templateService.DeleteTemplateAsync(id);
            
            if (!success)
            {
                return NotFound(new { error = "Template not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the template" });
        }
    }

    /// <summary>
    /// Import a template from Google Docs
    /// </summary>
    /// <param name="request">Google Docs import request</param>
    /// <returns>Imported template</returns>
    [HttpPost("import/google-docs")]
    [ProducesResponseType(typeof(ResumeTemplateDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<ActionResult<ResumeTemplateDto>> ImportFromGoogleDocs(
        [FromBody, Required] GoogleDocsImportRequestDto request)
    {
        try
        {
            var template = await _templateService.ImportFromGoogleDocsAsync(request);
            
            return CreatedAtAction(
                nameof(GetTemplate), 
                new { id = template.Id }, 
                template);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid Google Docs import request");
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to Google Docs");
            return Unauthorized(new { error = "Unable to access the specified Google Document" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing template from Google Docs");
            return StatusCode(500, new { error = "An error occurred while importing the template" });
        }
    }
}