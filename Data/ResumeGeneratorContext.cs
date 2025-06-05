using Microsoft.EntityFrameworkCore;
using ResumeGenerator.API.Models.Entities;
using ResumeGenerator.API.Models.Enums;

namespace ResumeGenerator.API.Data;

/// <summary>
/// Entity Framework database context for the Resume Generator application
/// </summary>
public class ResumeGeneratorContext : DbContext
{
    public ResumeGeneratorContext(DbContextOptions<ResumeGeneratorContext> options) : base(options)
    {
    }

    public DbSet<ResumeTemplate> ResumeTemplates { get; set; } = null!;
    public DbSet<ResumeJob> ResumeJobs { get; set; } = null!;
    public DbSet<ResumeJobLog> ResumeJobLogs { get; set; } = null!;
    public DbSet<ApiUsageStats> ApiUsageStats { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ResumeTemplate
        modelBuilder.Entity<ResumeTemplate>(entity =>
        {
            entity.HasIndex(e => e.Name).HasDatabaseName("IX_resume_templates_name");
            entity.HasIndex(e => e.IsPublic).HasDatabaseName("IX_resume_templates_is_public");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_resume_templates_created_at");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_resume_templates_is_active");

            entity.Property(e => e.Format)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Set up the relationship
            entity.HasMany(e => e.ResumeJobs)
                .WithOne(e => e.Template)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ResumeJob
        modelBuilder.Entity<ResumeJob>(entity =>
        {
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_resume_jobs_status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_resume_jobs_created_at");
            entity.HasIndex(e => e.TemplateId).HasDatabaseName("IX_resume_jobs_template_id");
            entity.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("IX_resume_jobs_status_created_at");

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.CurrentStep)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.OutputFormat)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Configure constraints
            entity.Property(e => e.ProgressPercentage)
                .HasDefaultValue(0);

            entity.HasCheckConstraint("CK_resume_jobs_progress_percentage", 
                "progress_percentage >= 0 AND progress_percentage <= 100");
        });

        // Configure ResumeJobLog
        modelBuilder.Entity<ResumeJobLog>(entity =>
        {
            entity.HasIndex(e => e.JobId).HasDatabaseName("IX_resume_job_logs_job_id");
            entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_resume_job_logs_timestamp");
            entity.HasIndex(e => e.Step).HasDatabaseName("IX_resume_job_logs_step");
            entity.HasIndex(e => e.IsError).HasDatabaseName("IX_resume_job_logs_is_error");

            entity.Property(e => e.Step)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ApiUsageStats
        modelBuilder.Entity<ApiUsageStats>(entity =>
        {
            entity.HasIndex(e => e.Date)
                .IsUnique()
                .HasDatabaseName("IX_api_usage_stats_date");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Add some default templates
        SeedDefaultTemplates(modelBuilder);
    }

    private static void SeedDefaultTemplates(ModelBuilder modelBuilder)
    {
        var modernTemplate = new ResumeTemplate
        {
            Id = Guid.Parse("a1b2c3d4-e5f6-7890-1234-567890abcdef"),
            Name = "Modern Professional",
            Description = "A clean, modern template suitable for most professional roles",
            Content = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}} - Resume</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 40px; background: #f8f9fa; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 20px rgba(0,0,0,0.1); }
        .header { text-align: center; border-bottom: 3px solid #007acc; padding-bottom: 20px; margin-bottom: 30px; }
        .name { font-size: 2.5em; font-weight: bold; color: #333; margin-bottom: 10px; }
        .contact { color: #666; font-size: 1.1em; }
        .section { margin-bottom: 30px; }
        .section-title { font-size: 1.4em; font-weight: bold; color: #007acc; border-bottom: 2px solid #007acc; padding-bottom: 5px; margin-bottom: 15px; }
        .experience-item, .education-item { margin-bottom: 20px; }
        .job-title { font-weight: bold; font-size: 1.2em; color: #333; }
        .company { color: #007acc; font-size: 1.1em; }
        .date-location { color: #666; font-style: italic; }
        .description { margin-top: 8px; line-height: 1.6; }
        .skills { display: flex; flex-wrap: wrap; gap: 10px; }
        .skill { background: #007acc; color: white; padding: 5px 15px; border-radius: 20px; font-size: 0.9em; }
        ul { padding-left: 20px; }
        li { margin-bottom: 5px; line-height: 1.5; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='name'>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}}</div>
            <div class='contact'>
                {{#if PersonalInfo.Email}}{{PersonalInfo.Email}}{{/if}}
                {{#if PersonalInfo.Phone}} • {{PersonalInfo.Phone}}{{/if}}
                {{#if PersonalInfo.Address}} • {{PersonalInfo.Address}}{{/if}}
            </div>
            {{#if PersonalInfo.LinkedInUrl}}<div><a href='{{PersonalInfo.LinkedInUrl}}'>LinkedIn</a></div>{{/if}}
        </div>
        
        {{#if PersonalInfo.ProfessionalSummary}}
        <div class='section'>
            <div class='section-title'>Professional Summary</div>
            <p>{{PersonalInfo.ProfessionalSummary}}</p>
        </div>
        {{/if}}
        
        {{#if Experience}}
        <div class='section'>
            <div class='section-title'>Professional Experience</div>
            {{#each Experience}}
            <div class='experience-item'>
                <div class='job-title'>{{JobTitle}}</div>
                <div class='company'>{{Company}}{{#if Location}} - {{Location}}{{/if}}</div>
                <div class='date-location'>{{StartDate}}{{#if EndDate}} - {{EndDate}}{{else}} - Present{{/if}}</div>
                {{#if Description}}<div class='description'>{{Description}}</div>{{/if}}
                {{#if Accomplishments}}
                <ul>
                    {{#each Accomplishments}}<li>{{this}}</li>{{/each}}
                </ul>
                {{/if}}
            </div>
            {{/each}}
        </div>
        {{/if}}
        
        {{#if Education}}
        <div class='section'>
            <div class='section-title'>Education</div>
            {{#each Education}}
            <div class='education-item'>
                <div class='job-title'>{{Degree}}{{#if FieldOfStudy}} in {{FieldOfStudy}}{{/if}}</div>
                <div class='company'>{{Institution}}{{#if Location}} - {{Location}}{{/if}}</div>
                {{#if EndDate}}<div class='date-location'>Graduated {{EndDate}}</div>{{/if}}
                {{#if Gpa}}<div>GPA: {{Gpa}}</div>{{/if}}
            </div>
            {{/each}}
        </div>
        {{/if}}
        
        {{#if Skills}}
        <div class='section'>
            <div class='section-title'>Skills</div>
            <div class='skills'>
                {{#each Skills}}<span class='skill'>{{this}}</span>{{/each}}
            </div>
        </div>
        {{/if}}
    </div>
</body>
</html>",
            Format = TemplateFormat.Html,
            TagsJson = "[\"professional\", \"modern\", \"clean\"]",
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var creativeTemplate = new ResumeTemplate
        {
            Id = Guid.Parse("b2c3d4e5-f6g7-8901-2345-678901bcdefg"),
            Name = "Creative Portfolio",
            Description = "A creative template for designers and creative professionals",
            Content = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}} - Creative Resume</title>
    <style>
        body { font-family: 'Arial', sans-serif; margin: 0; padding: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        .container { max-width: 850px; margin: 0 auto; background: white; border-radius: 15px; overflow: hidden; box-shadow: 0 20px 40px rgba(0,0,0,0.2); }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px; text-align: center; }
        .name { font-size: 3em; font-weight: 300; margin-bottom: 10px; text-shadow: 2px 2px 4px rgba(0,0,0,0.3); }
        .contact { font-size: 1.2em; opacity: 0.9; }
        .content { padding: 40px; }
        .section { margin-bottom: 35px; }
        .section-title { font-size: 1.6em; font-weight: bold; color: #667eea; margin-bottom: 20px; position: relative; }
        .section-title::after { content: ''; position: absolute; bottom: -5px; left: 0; width: 50px; height: 3px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        .creative-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
        .creative-item { background: #f8f9fa; padding: 20px; border-radius: 10px; border-left: 4px solid #667eea; }
        .item-title { font-weight: bold; color: #333; margin-bottom: 5px; }
        .item-subtitle { color: #667eea; margin-bottom: 10px; }
        .item-description { line-height: 1.6; font-size: 0.95em; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='name'>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}}</div>
            <div class='contact'>
                {{#if PersonalInfo.Email}}{{PersonalInfo.Email}}{{/if}}
                {{#if PersonalInfo.Phone}} • {{PersonalInfo.Phone}}{{/if}}
            </div>
        </div>
        
        <div class='content'>
            {{#if PersonalInfo.ProfessionalSummary}}
            <div class='section'>
                <div class='section-title'>About Me</div>
                <p style='font-size: 1.1em; line-height: 1.7; color: #666;'>{{PersonalInfo.ProfessionalSummary}}</p>
            </div>
            {{/if}}
            
            {{#if Experience}}
            <div class='section'>
                <div class='section-title'>Experience</div>
                <div class='creative-grid'>
                    {{#each Experience}}
                    <div class='creative-item'>
                        <div class='item-title'>{{JobTitle}}</div>
                        <div class='item-subtitle'>{{Company}}</div>
                        <div class='item-description'>{{Description}}</div>
                    </div>
                    {{/each}}
                </div>
            </div>
            {{/if}}
            
            {{#if Projects}}
            <div class='section'>
                <div class='section-title'>Featured Projects</div>
                <div class='creative-grid'>
                    {{#each Projects}}
                    <div class='creative-item'>
                        <div class='item-title'>{{Name}}</div>
                        <div class='item-description'>{{Description}}</div>
                    </div>
                    {{/each}}
                </div>
            </div>
            {{/if}}
        </div>
    </div>
</body>
</html>",
            Format = TemplateFormat.Html,
            TagsJson = "[\"creative\", \"portfolio\", \"design\"]",
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        modelBuilder.Entity<ResumeTemplate>().HasData(modernTemplate, creativeTemplate);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is ResumeTemplate template && entry.State == EntityState.Modified)
            {
                template.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is ResumeJob job && entry.State == EntityState.Modified)
            {
                job.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is ApiUsageStats stats && entry.State == EntityState.Modified)
            {
                stats.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}