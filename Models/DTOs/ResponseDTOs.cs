using ResumeGenerator.API.Models.Enums;

namespace ResumeGenerator.API.Models.DTOs;

/// <summary>
/// Response model for resume generation requests
/// </summary>
public class ResumeGenerationResponseDto
{
    /// <summary>
    /// Unique identifier for the generation job
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Current status of the generation job
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Estimated completion time
    /// </summary>
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>
    /// Message describing the current status
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Generated content (available when status is Completed)
    /// </summary>
    public ResumeContentDto? Content { get; set; }
}

/// <summary>
/// Generated resume content
/// </summary>
public class ResumeContentDto
{
    /// <summary>
    /// Primary generated content from Claude
    /// </summary>
    public string GeneratedContent { get; set; } = string.Empty;

    /// <summary>
    /// Generated cover letter (if requested)
    /// </summary>
    public string? CoverLetterContent { get; set; }

    /// <summary>
    /// Content format
    /// </summary>
    public OutputFormat Format { get; set; }

    /// <summary>
    /// AI review and suggestions from OpenAI (if requested)
    /// </summary>
    public AiReviewDto? AiReview { get; set; }

    /// <summary>
    /// Cover letter review and suggestions (if cover letter was generated)
    /// </summary>
    public CoverLetterReviewDto? CoverLetterReview { get; set; }

    /// <summary>
    /// Job match analysis (if job description was provided)
    /// </summary>
    public JobMatchAnalysisDto? JobMatchAnalysis { get; set; }

    /// <summary>
    /// Metadata about the generation process
    /// </summary>
    public GenerationMetadataDto Metadata { get; set; } = new();
}

/// <summary>
/// AI review and suggestions
/// </summary>
public class AiReviewDto
{
    /// <summary>
    /// Overall score (1-10)
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// Strengths identified in the resume
    /// </summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>
    /// Areas for improvement
    /// </summary>
    public List<string> ImprovementSuggestions { get; set; } = new();

    /// <summary>
    /// Specific recommendations by section
    /// </summary>
    public Dictionary<string, List<string>> SectionRecommendations { get; set; } = new();

    /// <summary>
    /// Keywords analysis
    /// </summary>
    public KeywordAnalysisDto? KeywordAnalysis { get; set; }

    /// <summary>
    /// General feedback
    /// </summary>
    public string? GeneralFeedback { get; set; }
}

/// <summary>
/// Keyword analysis results
/// </summary>
public class KeywordAnalysisDto
{
    /// <summary>
    /// Industry-relevant keywords found
    /// </summary>
    public List<string> RelevantKeywords { get; set; } = new();

    /// <summary>
    /// Suggested keywords to add
    /// </summary>
    public List<string> SuggestedKeywords { get; set; } = new();

    /// <summary>
    /// Keyword density score
    /// </summary>
    public double KeywordDensityScore { get; set; }
}

/// <summary>
/// Cover letter review and suggestions
/// </summary>
public class CoverLetterReviewDto
{
    /// <summary>
    /// Overall score for the cover letter (1-10)
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// Strengths identified in the cover letter
    /// </summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>
    /// Areas for improvement
    /// </summary>
    public List<string> ImprovementSuggestions { get; set; } = new();

    /// <summary>
    /// Tone and style feedback
    /// </summary>
    public string? ToneFeedback { get; set; }

    /// <summary>
    /// Personalization score (how well it addresses the specific job/company)
    /// </summary>
    public int PersonalizationScore { get; set; }

    /// <summary>
    /// General feedback
    /// </summary>
    public string? GeneralFeedback { get; set; }
}

/// <summary>
/// Job match analysis results
/// </summary>
public class JobMatchAnalysisDto
{
    /// <summary>
    /// Overall match score percentage (0-100)
    /// </summary>
    public int OverallMatchScore { get; set; }

    /// <summary>
    /// Skills match analysis
    /// </summary>
    public SkillsMatchDto SkillsMatch { get; set; } = new();

    /// <summary>
    /// Experience match analysis
    /// </summary>
    public ExperienceMatchDto ExperienceMatch { get; set; } = new();

    /// <summary>
    /// Keywords that align with job requirements
    /// </summary>
    public List<string> MatchingKeywords { get; set; } = new();

    /// <summary>
    /// Important keywords missing from resume
    /// </summary>
    public List<string> MissingKeywords { get; set; } = new();

    /// <summary>
    /// Recommendations for improving job match
    /// </summary>
    public List<string> ImprovementRecommendations { get; set; } = new();

    /// <summary>
    /// Qualification alignment score
    /// </summary>
    public int QualificationScore { get; set; }

    /// <summary>
    /// Company culture fit indicators
    /// </summary>
    public List<string> CultureFitIndicators { get; set; } = new();
}

/// <summary>
/// Skills match analysis
/// </summary>
public class SkillsMatchDto
{
    /// <summary>
    /// Required skills that the candidate has
    /// </summary>
    public List<string> MatchingRequiredSkills { get; set; } = new();

    /// <summary>
    /// Required skills that the candidate is missing
    /// </summary>
    public List<string> MissingRequiredSkills { get; set; } = new();

    /// <summary>
    /// Preferred skills that the candidate has
    /// </summary>
    public List<string> MatchingPreferredSkills { get; set; } = new();

    /// <summary>
    /// Skills match percentage for required skills
    /// </summary>
    public int RequiredSkillsMatchPercentage { get; set; }

    /// <summary>
    /// Skills match percentage for preferred skills
    /// </summary>
    public int PreferredSkillsMatchPercentage { get; set; }
}

/// <summary>
/// Experience match analysis
/// </summary>
public class ExperienceMatchDto
{
    /// <summary>
    /// Whether experience level matches job requirements
    /// </summary>
    public bool ExperienceLevelMatch { get; set; }

    /// <summary>
    /// Years of relevant experience
    /// </summary>
    public double RelevantExperienceYears { get; set; }

    /// <summary>
    /// Industry experience match
    /// </summary>
    public bool IndustryMatch { get; set; }

    /// <summary>
    /// Role progression alignment
    /// </summary>
    public string RoleProgressionFeedback { get; set; } = string.Empty;

    /// <summary>
    /// Matching responsibilities from previous roles
    /// </summary>
    public List<string> MatchingResponsibilities { get; set; } = new();
}

/// <summary>
/// Metadata about the generation process
/// </summary>
public class GenerationMetadataDto
{
    /// <summary>
    /// Template used for generation
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Claude API version used
    /// </summary>
    public string ClaudeApiVersion { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI model used for review (if applicable)
    /// </summary>
    public string? OpenAiModel { get; set; }

    /// <summary>
    /// Token usage statistics
    /// </summary>
    public TokenUsageDto TokenUsage { get; set; } = new();
}

/// <summary>
/// Token usage statistics
/// </summary>
public class TokenUsageDto
{
    /// <summary>
    /// Input tokens used for Claude
    /// </summary>
    public int ClaudeInputTokens { get; set; }

    /// <summary>
    /// Output tokens generated by Claude
    /// </summary>
    public int ClaudeOutputTokens { get; set; }

    /// <summary>
    /// Input tokens used for OpenAI (if applicable)
    /// </summary>
    public int? OpenAiInputTokens { get; set; }

    /// <summary>
    /// Output tokens generated by OpenAI (if applicable)
    /// </summary>
    public int? OpenAiOutputTokens { get; set; }
}

/// <summary>
/// Job status information
/// </summary>
public class ResumeJobStatusDto
{
    /// <summary>
    /// Job identifier
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Current job status
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Current step description
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// Job creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Estimated completion time
    /// </summary>
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>
    /// Error message (if status is Failed)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Job summary for listing
/// </summary>
public class ResumeJobSummaryDto
{
    /// <summary>
    /// Job identifier
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Template used
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Person's name from the request
    /// </summary>
    public string PersonName { get; set; } = string.Empty;

    /// <summary>
    /// Job status
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Completion timestamp (if completed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Processing time in milliseconds (if completed)
    /// </summary>
    public long? ProcessingTimeMs { get; set; }
}

/// <summary>
/// Resume template information
/// </summary>
public class ResumeTemplateDto
{
    /// <summary>
    /// Template identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Template content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Template format
    /// </summary>
    public TemplateFormat Format { get; set; }

    /// <summary>
    /// Template tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Whether the template is publicly available
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }
}

/// <summary>
/// Paginated result wrapper
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Items in the current page
    /// </summary>
    public List<T> Items { get; set; } = new();
}