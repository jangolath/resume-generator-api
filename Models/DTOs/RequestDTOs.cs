using ResumeGenerator.API.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ResumeGenerator.API.Models.DTOs;

/// <summary>
/// Request model for generating a resume
/// </summary>
public class ResumeGenerationRequestDto
{
    /// <summary>
    /// The ID of the template to use for generation
    /// </summary>
    [Required]
    public Guid TemplateId { get; set; }

/// <summary>
/// Job description for tailoring resume and cover letter
/// </summary>
public class JobDescriptionDto
{
    /// <summary>
    /// Job title
    /// </summary>
    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// Company name
    /// </summary>
    [Required, MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// Job location
    /// </summary>
    [MaxLength(100)]
    public string? Location { get; set; }

    /// <summary>
    /// Job type (Full-time, Part-time, Contract, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? JobType { get; set; }

    /// <summary>
    /// Salary range or compensation details
    /// </summary>
    [MaxLength(100)]
    public string? SalaryRange { get; set; }

    /// <summary>
    /// Full job description text
    /// </summary>
    [Required, MaxLength(10000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Required qualifications and skills
    /// </summary>
    public List<string> RequiredQualifications { get; set; } = new();

    /// <summary>
    /// Preferred qualifications and skills
    /// </summary>
    public List<string> PreferredQualifications { get; set; } = new();

    /// <summary>
    /// Key responsibilities
    /// </summary>
    public List<string> Responsibilities { get; set; } = new();

    /// <summary>
    /// Required technical skills
    /// </summary>
    public List<string> RequiredSkills { get; set; } = new();

    /// <summary>
    /// Nice-to-have technical skills
    /// </summary>
    public List<string> PreferredSkills { get; set; } = new();

    /// <summary>
    /// Industry or sector
    /// </summary>
    [MaxLength(100)]
    public string? Industry { get; set; }

    /// <summary>
    /// Experience level required (Entry, Mid, Senior, Executive)
    /// </summary>
    [MaxLength(50)]
    public string? ExperienceLevel { get; set; }

    /// <summary>
    /// Remote work policy
    /// </summary>
    [MaxLength(100)]
    public string? RemotePolicy { get; set; }

    /// <summary>
    /// Additional job posting URL for reference
    /// </summary>
    [Url, MaxLength(500)]
    public string? JobPostingUrl { get; set; }
}

    /// <summary>
    /// Personal information for the resume
    /// </summary>
    [Required]
    public PersonalInfoDto PersonalInfo { get; set; } = new();

    /// <summary>
    /// Professional experience entries
    /// </summary>
    public List<ExperienceDto> Experience { get; set; } = new();

    /// <summary>
    /// Education entries
    /// </summary>
    public List<EducationDto> Education { get; set; } = new();

    /// <summary>
    /// Skills and competencies
    /// </summary>
    public List<string> Skills { get; set; } = new();

    /// <summary>
    /// Certifications and awards
    /// </summary>
    public List<CertificationDto> Certifications { get; set; } = new();

    /// <summary>
    /// Projects to highlight
    /// </summary>
    public List<ProjectDto> Projects { get; set; } = new();

    /// <summary>
    /// Additional sections (custom content)
    /// </summary>
    public Dictionary<string, object> AdditionalSections { get; set; } = new();

    /// <summary>
    /// Preferred output format
    /// </summary>
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Html;

    /// <summary>
    /// Whether to use OpenAI for review and suggestions
    /// </summary>
    public bool IncludeAiReview { get; set; } = true;

    /// <summary>
    /// Whether to generate a cover letter
    /// </summary>
    public bool GenerateCoverLetter { get; set; } = false;

    /// <summary>
    /// Job description to tailor the resume and cover letter for
    /// </summary>
    public JobDescriptionDto? JobDescription { get; set; }

    /// <summary>
    /// Custom instructions for the AI generation
    /// </summary>
    public string? CustomInstructions { get; set; }
}

/// <summary>
/// Personal information section
/// </summary>
public class PersonalInfoDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(255)]
    public string? Email { get; set; }

    [Phone, MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [Url, MaxLength(255)]
    public string? LinkedInUrl { get; set; }

    [Url, MaxLength(255)]
    public string? GitHubUrl { get; set; }

    [Url, MaxLength(255)]
    public string? PersonalWebsite { get; set; }

    [MaxLength(1000)]
    public string? ProfessionalSummary { get; set; }
}

/// <summary>
/// Work experience entry
/// </summary>
public class ExperienceDto
{
    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Location { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsCurrentPosition { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public List<string> Accomplishments { get; set; } = new();

    public List<string> Technologies { get; set; } = new();
}

/// <summary>
/// Education entry
/// </summary>
public class EducationDto
{
    [Required, MaxLength(200)]
    public string Institution { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Degree { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? FieldOfStudy { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Range(0.0, 4.0)]
    public double? Gpa { get; set; }

    public List<string> Achievements { get; set; } = new();
}

/// <summary>
/// Certification or award entry
/// </summary>
public class CertificationDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? IssuingOrganization { get; set; }

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    [MaxLength(100)]
    public string? CredentialId { get; set; }

    [Url, MaxLength(500)]
    public string? VerificationUrl { get; set; }
}

/// <summary>
/// Project entry
/// </summary>
public class ProjectDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Url, MaxLength(500)]
    public string? ProjectUrl { get; set; }

    [Url, MaxLength(500)]
    public string? GitHubUrl { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public List<string> Technologies { get; set; } = new();

    public List<string> KeyFeatures { get; set; } = new();
}

/// <summary>
/// Request for creating a new template
/// </summary>
public class CreateTemplateRequestDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public TemplateFormat Format { get; set; } = TemplateFormat.Html;

    public List<string> Tags { get; set; } = new();

    public bool IsPublic { get; set; } = false;
}

/// <summary>
/// Request for updating an existing template
/// </summary>
public class UpdateTemplateRequestDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? Content { get; set; }

    public TemplateFormat? Format { get; set; }

    public List<string>? Tags { get; set; }

    public bool? IsPublic { get; set; }
}

/// <summary>
/// Request for importing a template from Google Docs
/// </summary>
public class GoogleDocsImportRequestDto
{
    [Required, Url]
    public string DocumentUrl { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<string> Tags { get; set; } = new();

    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Google service account credentials (JSON)
    /// </summary>
    public string? ServiceAccountCredentials { get; set; }
}