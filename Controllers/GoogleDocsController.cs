using Microsoft.AspNetCore.Mvc;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace ResumeGenerator.API.Controllers;

/// <summary>
/// Controller for Google Docs integration operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GoogleDocsController : ControllerBase
{
    private readonly IGoogleDocsService _googleDocsService;
    private readonly IResumeTemplateService _templateService;
    private readonly ILogger<GoogleDocsController> _logger;

    public GoogleDocsController(
        IGoogleDocsService googleDocsService,
        IResumeTemplateService templateService,
        ILogger<GoogleDocsController> logger)
    {
        _googleDocsService = googleDocsService;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available resume templates from Google Drive folder
    /// </summary>
    /// <returns>List of available Google Docs templates</returns>
    /// <response code="200">Templates retrieved successfully</response>
    /// <response code="500">Error accessing Google Drive</response>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IEnumerable<GoogleDocsTemplateDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<GoogleDocsTemplateDto>>> GetAvailableTemplates()
    {
        try
        {
            _logger.LogInformation("Retrieving available Google Docs templates");
            
            var templates = await _googleDocsService.GetTemplatesFromFolderAsync();
            
            _logger.LogInformation("Found {Count} Google Docs templates", templates.Count());
            
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Google Docs templates");
            return StatusCode(500, new { error = "Failed to retrieve templates from Google Drive" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific Google Docs template
    /// </summary>
    /// <param name="documentId">Google Document ID</param>
    /// <returns>Detailed template information</returns>
    /// <response code="200">Template details retrieved successfully</response>
    /// <response code="404">Template not found</response>
    /// <response code="500">Error accessing Google Docs</response>
    [HttpGet("templates/{documentId}")]
    [ProducesResponseType(typeof(GoogleDocsTemplateDetailDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<GoogleDocsTemplateDetailDto>> GetTemplateDetail(string documentId)
    {
        try
        {
            if (!_googleDocsService.IsValidGoogleDocsUrl(documentId))
            {
                return BadRequest(new { error = "Invalid document ID format" });
            }

            _logger.LogInformation("Retrieving details for Google Docs template: {DocumentId}", documentId);
            
            var templateDetail = await _googleDocsService.GetTemplateDetailAsync(documentId);
            
            return Ok(templateDetail);
        }
        catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Google Docs template not found: {DocumentId}", documentId);
            return NotFound(new { error = "Template not found or not accessible" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Google Docs template details: {DocumentId}", documentId);
            return StatusCode(500, new { error = "Failed to retrieve template details" });
        }
    }

    /// <summary>
    /// Import a Google Docs template and convert it to a resume template
    /// </summary>
    /// <param name="request">Import request with template information</param>
    /// <returns>Created resume template</returns>
    /// <response code="201">Template imported and created successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Google Docs template not found</response>
    /// <response code="500">Error during import process</response>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ResumeTemplateDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ResumeTemplateDto>> ImportTemplate(
        [FromBody, Required] ImportGoogleDocsTemplateRequestDto request)
    {
        try
        {
            if (!_googleDocsService.IsValidGoogleDocsUrl(request.DocumentId))
            {
                return BadRequest(new { error = "Invalid Google Docs URL or document ID" });
            }

            _logger.LogInformation("Importing Google Docs template: {DocumentId} as '{TemplateName}'", 
                request.DocumentId, request.TemplateName);

            // Get the Google Docs content
            var content = await _googleDocsService.ImportTemplateFromGoogleDocsAsync(request.DocumentId);

            // Process placeholders if requested
            if (request.AutoDetectPlaceholders)
            {
                content = ProcessPlaceholders(content, request.CustomPlaceholders);
            }

            // Create the resume template
            var createRequest = new CreateTemplateRequestDto
            {
                Name = request.TemplateName,
                Description = request.Description ?? $"Imported from Google Docs on {DateTime.UtcNow:yyyy-MM-dd}",
                Content = content,
                Format = Models.Enums.TemplateFormat.Html,
                Tags = request.Tags.Concat(new[] { "google-docs", "imported" }).ToList(),
                IsPublic = request.IsPublic
            };

            var template = await _templateService.CreateTemplateAsync(createRequest);

            _logger.LogInformation("Successfully imported Google Docs template {DocumentId} as template {TemplateId}", 
                request.DocumentId, template.Id);

            return CreatedAtAction(
                nameof(ResumeGenerator.API.Controllers.TemplateController.GetTemplate),
                "Template",
                new { id = template.Id },
                template);
        }
        catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Google Docs template not found during import: {DocumentId}", request.DocumentId);
            return NotFound(new { error = "Google Docs template not found or not accessible" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to Google Docs template: {DocumentId}", request.DocumentId);
            return Unauthorized(new { error = "Unable to access the specified Google Document. Check permissions." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid import request for Google Docs template: {DocumentId}", request.DocumentId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Google Docs template: {DocumentId}", request.DocumentId);
            return StatusCode(500, new { error = "Failed to import template from Google Docs" });
        }
    }

    /// <summary>
    /// Preview a Google Docs template without importing it
    /// </summary>
    /// <param name="documentId">Google Document ID</param>
    /// <returns>Preview of the template content</returns>
    /// <response code="200">Template preview generated successfully</response>
    /// <response code="404">Template not found</response>
    /// <response code="500">Error generating preview</response>
    [HttpGet("preview/{documentId}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> PreviewTemplate(string documentId)
    {
        try
        {
            if (!_googleDocsService.IsValidGoogleDocsUrl(documentId))
            {
                return BadRequest(new { error = "Invalid document ID format" });
            }

            _logger.LogInformation("Generating preview for Google Docs template: {DocumentId}", documentId);

            var templateDetail = await _googleDocsService.GetTemplateDetailAsync(documentId);
            
            // Extract potential placeholders for preview
            var placeholders = ExtractPlaceholders(templateDetail.HtmlContent);

            var preview = new
            {
                documentId = templateDetail.Id,
                name = templateDetail.Name,
                description = templateDetail.Description,
                contentPreview = templateDetail.ContentPreview,
                detectedPlaceholders = placeholders,
                lastModified = templateDetail.ModifiedTime,
                fileSize = templateDetail.FileSize,
                webViewLink = templateDetail.WebViewLink
            };

            return Ok(preview);
        }
        catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Google Docs template not found for preview: {DocumentId}", documentId);
            return NotFound(new { error = "Template not found or not accessible" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview for Google Docs template: {DocumentId}", documentId);
            return StatusCode(500, new { error = "Failed to generate template preview" });
        }
    }

    /// <summary>
    /// Test Google Docs API connectivity
    /// </summary>
    /// <returns>Connection status</returns>
    [HttpGet("test-connection")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult> TestConnection()
    {
        try
        {
            _logger.LogInformation("Testing Google Docs API connectivity");

            var templates = await _googleDocsService.GetTemplatesFromFolderAsync();
            var isConnected = true;
            var templateCount = templates.Count();

            return Ok(new
            {
                isConnected,
                templateCount,
                message = $"Successfully connected to Google Drive. Found {templateCount} templates.",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Docs API connectivity test failed");
            return Ok(new
            {
                isConnected = false,
                templateCount = 0,
                error = ex.Message,
                message = "Failed to connect to Google Drive. Check your configuration.",
                timestamp = DateTime.UtcNow
            });
        }
    }

    private string ProcessPlaceholders(string content, Dictionary<string, string> customPlaceholders)
    {
        // Replace custom placeholders first
        foreach (var placeholder in customPlaceholders)
        {
            content = content.Replace(placeholder.Key, $"{{{{{placeholder.Value}}}}}");
        }

        // Auto-detect common patterns and convert them to Handlebars placeholders
        var patterns = new Dictionary<string, string>
        {
            [@"\[First Name\]"] = "{{PersonalInfo.FirstName}}",
            [@"\[Last Name\]"] = "{{PersonalInfo.LastName}}",
            [@"\[Full Name\]"] = "{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}}",
            [@"\[Email\]"] = "{{PersonalInfo.Email}}",
            [@"\[Phone\]"] = "{{PersonalInfo.Phone}}",
            [@"\[Address\]"] = "{{PersonalInfo.Address}}",
            [@"\[LinkedIn\]"] = "{{PersonalInfo.LinkedInUrl}}",
            [@"\[GitHub\]"] = "{{PersonalInfo.GitHubUrl}}",
            [@"\[Website\]"] = "{{PersonalInfo.PersonalWebsite}}",
            [@"\[Summary\]"] = "{{PersonalInfo.ProfessionalSummary}}",
            [@"\[Professional Summary\]"] = "{{PersonalInfo.ProfessionalSummary}}"
        };

        foreach (var pattern in patterns)
        {
            content = System.Text.RegularExpressions.Regex.Replace(
                content, pattern.Key, pattern.Value, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return content;
    }

    private List<string> ExtractPlaceholders(string content)
    {
        var placeholders = new List<string>();
        
        // Look for bracketed text that looks like placeholders
        var bracketMatches = System.Text.RegularExpressions.Regex.Matches(
            content, @"\[([^\]]+)\]");
        
        foreach (System.Text.RegularExpressions.Match match in bracketMatches)
        {
            placeholders.Add(match.Value);
        }

        // Look for existing Handlebars placeholders
        var handlebarsMatches = System.Text.RegularExpressions.Regex.Matches(
            content, @"\{\{([^}]+)\}\}");
        
        foreach (System.Text.RegularExpressions.Match match in handlebarsMatches)
        {
            placeholders.Add(match.Value);
        }

        return placeholders.Distinct().ToList();
    }
}