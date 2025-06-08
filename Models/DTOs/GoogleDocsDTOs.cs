using System.ComponentModel.DataAnnotations;

namespace ResumeGenerator.API.Models.DTOs;

/// <summary>
/// Google Docs template basic information
/// </summary>
public class GoogleDocsTemplateDto
{
    /// <summary>
    /// Google Document ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Document name/title
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Document description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the document was created
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// When the document was last modified
    /// </summary>
    public DateTime ModifiedTime { get; set; }

    /// <summary>
    /// Web view link for the document
    /// </summary>
    public string WebViewLink { get; set; } = string.Empty;

    /// <summary>
    /// Direct document URL
    /// </summary>
    public string DocumentUrl { get; set; } = string.Empty;
}

/// <summary>
/// Detailed Google Docs template information including content
/// </summary>
public class GoogleDocsTemplateDetailDto : GoogleDocsTemplateDto
{
    /// <summary>
    /// Document content converted to HTML
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Document content as plain text
    /// </summary>
    public string PlainTextContent { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Preview of the content (first 200 characters)
    /// </summary>
    public string ContentPreview => PlainTextContent.Length > 200 
        ? PlainTextContent.Substring(0, 200) + "..." 
        : PlainTextContent;
}

/// <summary>
/// Request to import a template from Google Docs and convert it to a resume template
/// </summary>
public class ImportGoogleDocsTemplateRequestDto
{
    /// <summary>
    /// Google Document ID or URL
    /// </summary>
    [Required]
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Name for the imported template
    /// </summary>
    [Required, MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Description for the imported template
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Tags for categorizing the template
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Whether the template should be publicly available
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Whether to automatically detect and convert placeholder variables
    /// </summary>
    public bool AutoDetectPlaceholders { get; set; } = true;

    /// <summary>
    /// Custom placeholder mapping (original text -> variable name)
    /// </summary>
    public Dictionary<string, string> CustomPlaceholders { get; set; } = new();
}