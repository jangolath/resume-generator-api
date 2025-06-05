namespace ResumeGenerator.API.Models.Enums;

/// <summary>
/// Status of a resume generation job
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job is queued and waiting to be processed
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently being processed
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed due to an error
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job was cancelled by user or system
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// Output format for generated resumes
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// HTML format with CSS styling
    /// </summary>
    Html = 0,

    /// <summary>
    /// Google Docs compatible format
    /// </summary>
    GoogleDocs = 1,

    /// <summary>
    /// Markdown format
    /// </summary>
    Markdown = 2,

    /// <summary>
    /// Plain text format
    /// </summary>
    PlainText = 3,

    /// <summary>
    /// LaTeX format
    /// </summary>
    LaTeX = 4
}

/// <summary>
/// Template format types
/// </summary>
public enum TemplateFormat
{
    /// <summary>
    /// HTML template with placeholders
    /// </summary>
    Html = 0,

    /// <summary>
    /// Google Docs template
    /// </summary>
    GoogleDocs = 1,

    /// <summary>
    /// Markdown template
    /// </summary>
    Markdown = 2,

    /// <summary>
    /// LaTeX template
    /// </summary>
    LaTeX = 3,

    /// <summary>
    /// Custom template format
    /// </summary>
    Custom = 4
}

/// <summary>
/// Processing step in the resume generation pipeline
/// </summary>
public enum ProcessingStep
{
    /// <summary>
    /// Validating input data
    /// </summary>
    Validation = 0,

    /// <summary>
    /// Loading template
    /// </summary>
    LoadingTemplate = 1,

    /// <summary>
    /// Generating content with Claude
    /// </summary>
    ClaudeGeneration = 2,

    /// <summary>
    /// Reviewing with OpenAI
    /// </summary>
    OpenAiReview = 3,

    /// <summary>
    /// Formatting output
    /// </summary>
    Formatting = 4,

    /// <summary>
    /// Finalizing and saving
    /// </summary>
    Finalizing = 5
}

/// <summary>
/// Experience level categories
/// </summary>
public enum ExperienceLevel
{
    /// <summary>
    /// Entry level (0-2 years)
    /// </summary>
    EntryLevel = 0,

    /// <summary>
    /// Junior level (2-5 years)
    /// </summary>
    Junior = 1,

    /// <summary>
    /// Mid level (5-8 years)
    /// </summary>
    Mid = 2,

    /// <summary>
    /// Senior level (8-15 years)
    /// </summary>
    Senior = 3,

    /// <summary>
    /// Executive level (15+ years)
    /// </summary>
    Executive = 4
}

/// <summary>
/// Industry categories for resume optimization
/// </summary>
public enum Industry
{
    /// <summary>
    /// Technology and Software
    /// </summary>
    Technology = 0,

    /// <summary>
    /// Healthcare and Medical
    /// </summary>
    Healthcare = 1,

    /// <summary>
    /// Finance and Banking
    /// </summary>
    Finance = 2,

    /// <summary>
    /// Education and Academia
    /// </summary>
    Education = 3,

    /// <summary>
    /// Marketing and Advertising
    /// </summary>
    Marketing = 4,

    /// <summary>
    /// Sales and Business Development
    /// </summary>
    Sales = 5,

    /// <summary>
    /// Human Resources
    /// </summary>
    HumanResources = 6,

    /// <summary>
    /// Operations and Logistics
    /// </summary>
    Operations = 7,

    /// <summary>
    /// Creative and Design
    /// </summary>
    Creative = 8,

    /// <summary>
    /// Legal and Compliance
    /// </summary>
    Legal = 9,

    /// <summary>
    /// Engineering and Manufacturing
    /// </summary>
    Engineering = 10,

    /// <summary>
    /// Consulting and Professional Services
    /// </summary>
    Consulting = 11,

    /// <summary>
    /// Other industry
    /// </summary>
    Other = 12
}