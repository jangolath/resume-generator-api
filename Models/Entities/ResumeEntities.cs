using ResumeGenerator.API.Models.Enums;
using ResumeGenerator.API.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeGenerator.API.Models.Entities;

/// <summary>
/// Resume template entity
/// </summary>
[Table("resume_templates")]
public class ResumeTemplate
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("format")]
    public TemplateFormat Format { get; set; } = TemplateFormat.Html;

    [Column("tags")]
    public string TagsJson { get; set; } = "[]";

    [Column("is_public")]
    public bool IsPublic { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("usage_count")]
    public int UsageCount { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<ResumeJob> ResumeJobs { get; set; } = new List<ResumeJob>();

    // Helper property for tags
    [NotMapped]
    public List<string> Tags
    {
        get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        set => TagsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}

/// <summary>
/// Resume generation job entity
/// </summary>
[Table("resume_jobs")]
public class ResumeJob
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("template_id")]
    public Guid TemplateId { get; set; }

    [Column("status")]
    public JobStatus Status { get; set; } = JobStatus.Pending;

    [Column("progress_percentage")]
    public int ProgressPercentage { get; set; } = 0;

    [Column("current_step")]
    public ProcessingStep? CurrentStep { get; set; }

    [MaxLength(500)]
    [Column("current_step_description")]
    public string? CurrentStepDescription { get; set; }

    [Column("input_data")]
    public string InputDataJson { get; set; } = string.Empty;

    [Column("generated_content")]
    public string? GeneratedContent { get; set; }

    [Column("cover_letter_content")]
    public string? CoverLetterContent { get; set; }

    [Column("ai_review")]
    public string? AiReviewJson { get; set; }

    [Column("cover_letter_review")]
    public string? CoverLetterReviewJson { get; set; }

    [Column("job_match_analysis")]
    public string? JobMatchAnalysisJson { get; set; }

    [Column("output_format")]
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Html;

    [Column("include_ai_review")]
    public bool IncludeAiReview { get; set; } = true;

    [Column("generate_cover_letter")]
    public bool GenerateCoverLetter { get; set; } = false;

    [Column("job_description")]
    public string? JobDescriptionJson { get; set; }

    [MaxLength(1000)]
    [Column("custom_instructions")]
    public string? CustomInstructions { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("estimated_completion")]
    public DateTime? EstimatedCompletion { get; set; }

    [Column("processing_time_ms")]
    public long? ProcessingTimeMs { get; set; }

    [MaxLength(1000)]
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("error_details")]
    public string? ErrorDetailsJson { get; set; }

    // Token usage tracking
    [Column("claude_input_tokens")]
    public int? ClaudeInputTokens { get; set; }

    [Column("claude_output_tokens")]
    public int? ClaudeOutputTokens { get; set; }

    [Column("openai_input_tokens")]
    public int? OpenAiInputTokens { get; set; }

    [Column("openai_output_tokens")]
    public int? OpenAiOutputTokens { get; set; }

    // API versions used
    [MaxLength(50)]
    [Column("claude_api_version")]
    public string? ClaudeApiVersion { get; set; }

    [MaxLength(50)]
    [Column("openai_model")]
    public string? OpenAiModel { get; set; }

    // Navigation properties
    [ForeignKey("TemplateId")]
    public virtual ResumeTemplate Template { get; set; } = null!;

    // Helper properties for complex JSON data
    [NotMapped]
    public ResumeGenerationRequestDto? InputData
    {
        get => string.IsNullOrEmpty(InputDataJson) ? null : 
               System.Text.Json.JsonSerializer.Deserialize<ResumeGenerationRequestDto>(InputDataJson);
        set => InputDataJson = value == null ? string.Empty : System.Text.Json.JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public AiReviewDto? AiReview
    {
        get => string.IsNullOrEmpty(AiReviewJson) ? null : 
               System.Text.Json.JsonSerializer.Deserialize<AiReviewDto>(AiReviewJson);
        set => AiReviewJson = value == null ? null : System.Text.Json.JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public CoverLetterReviewDto? CoverLetterReview
    {
        get => string.IsNullOrEmpty(CoverLetterReviewJson) ? null : 
               System.Text.Json.JsonSerializer.Deserialize<CoverLetterReviewDto>(CoverLetterReviewJson);
        set => CoverLetterReviewJson = value == null ? null : System.Text.Json.JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public JobMatchAnalysisDto? JobMatchAnalysis
    {
        get => string.IsNullOrEmpty(JobMatchAnalysisJson) ? null : 
               System.Text.Json.JsonSerializer.Deserialize<JobMatchAnalysisDto>(JobMatchAnalysisJson);
        set => JobMatchAnalysisJson = value == null ? null : System.Text.Json.JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public JobDescriptionDto? JobDescription
    {
        get => string.IsNullOrEmpty(JobDescriptionJson) ? null : 
               System.Text.Json.JsonSerializer.Deserialize<JobDescriptionDto>(JobDescriptionJson);
        set => JobDescriptionJson = value == null ? null : System.Text.Json.JsonSerializer.Serialize(value);
    }
}

/// <summary>
/// Job execution log for detailed tracking
/// </summary>
[Table("resume_job_logs")]
public class ResumeJobLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("job_id")]
    public Guid JobId { get; set; }

    [Column("step")]
    public ProcessingStep Step { get; set; }

    [MaxLength(500)]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("details")]
    public string? DetailsJson { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("duration_ms")]
    public long? DurationMs { get; set; }

    [Column("is_error")]
    public bool IsError { get; set; } = false;

    // Navigation properties
    [ForeignKey("JobId")]
    public virtual ResumeJob Job { get; set; } = null!;
}

/// <summary>
/// API usage statistics
/// </summary>
[Table("api_usage_stats")]
public class ApiUsageStats
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("total_jobs")]
    public int TotalJobs { get; set; } = 0;

    [Column("successful_jobs")]
    public int SuccessfulJobs { get; set; } = 0;

    [Column("failed_jobs")]
    public int FailedJobs { get; set; } = 0;

    [Column("total_claude_tokens")]
    public long TotalClaudeTokens { get; set; } = 0;

    [Column("total_openai_tokens")]
    public long TotalOpenAiTokens { get; set; } = 0;

    [Column("average_processing_time_ms")]
    public double AverageProcessingTimeMs { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}