using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumeGenerator.API.Data;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Enums;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ResumeGenerator.API.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the Resume Generation API
/// </summary>
public class ResumeGenerationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ResumeGenerationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ResumeGeneratorContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ResumeGeneratorContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });

                // Build service provider and ensure database is created
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ResumeGeneratorContext>();
                context.Database.EnsureCreated();
            });

            // Use test environment
            builder.UseEnvironment("Testing");
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetTemplates_ReturnsDefaultTemplates()
    {
        // Act
        var response = await _client.GetAsync("/api/template");

        // Assert
        response.EnsureSuccessStatusCode();
        var templates = await response.Content.ReadFromJsonAsync<List<ResumeTemplateDto>>();
        
        Assert.NotNull(templates);
        Assert.True(templates.Count >= 2); // Should have at least the seeded templates
        Assert.Contains(templates, t => t.Name == "Modern Professional");
        Assert.Contains(templates, t => t.Name == "Creative Portfolio");
    }

    [Fact]
    public async Task GetTemplate_ValidId_ReturnsTemplate()
    {
        // Arrange - First get the list of templates to get a valid ID
        var templatesResponse = await _client.GetAsync("/api/template");
        var templates = await templatesResponse.Content.ReadFromJsonAsync<List<ResumeTemplateDto>>();
        var templateId = templates!.First().Id;

        // Act
        var response = await _client.GetAsync($"/api/template/{templateId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var template = await response.Content.ReadFromJsonAsync<ResumeTemplateDto>();
        
        Assert.NotNull(template);
        Assert.Equal(templateId, template.Id);
        Assert.NotEmpty(template.Content);
    }

    [Fact]
    public async Task GetTemplate_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/template/{invalidId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTemplate_ValidRequest_ReturnsCreatedTemplate()
    {
        // Arrange
        var createRequest = new CreateTemplateRequestDto
        {
            Name = "Test Template",
            Description = "A test template for integration testing",
            Content = "<html><body><h1>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}}</h1></body></html>",
            Format = TemplateFormat.Html,
            Tags = new List<string> { "test", "integration" },
            IsPublic = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/template", createRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var createdTemplate = await response.Content.ReadFromJsonAsync<ResumeTemplateDto>();
        
        Assert.NotNull(createdTemplate);
        Assert.Equal(createRequest.Name, createdTemplate.Name);
        Assert.Equal(createRequest.Description, createdTemplate.Description);
        Assert.Equal(createRequest.Content, createdTemplate.Content);
        Assert.Equal(createRequest.Format, createdTemplate.Format);
        Assert.False(createdTemplate.IsPublic);
    }

    [Fact]
    public async Task GenerateResume_ValidRequest_ReturnsJobResponse()
    {
        // Arrange - First get a template ID
        var templatesResponse = await _client.GetAsync("/api/template");
        var templates = await templatesResponse.Content.ReadFromJsonAsync<List<ResumeTemplateDto>>();
        var templateId = templates!.First().Id;

        var resumeRequest = new ResumeGenerationRequestDto
        {
            TemplateId = templateId,
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "Integration",
                LastName = "Test",
                Email = "integration.test@example.com",
                Phone = "+1-555-TEST",
                ProfessionalSummary = "A test professional summary for integration testing"
            },
            Experience = new List<ExperienceDto>
            {
                new()
                {
                    JobTitle = "Senior Software Engineer",
                    Company = "Test Corp",
                    Location = "Test City, TC",
                    StartDate = DateTime.Now.AddYears(-3),
                    EndDate = DateTime.Now.AddMonths(-1),
                    Description = "Led development of test applications",
                    Accomplishments = new List<string>
                    {
                        "Improved test coverage by 85%",
                        "Reduced integration test runtime by 40%"
                    },
                    Technologies = new List<string> { "C#", ".NET", "xUnit", "ASP.NET Core" }
                }
            },
            Education = new List<EducationDto>
            {
                new()
                {
                    Institution = "Test University",
                    Degree = "Master of Science",
                    FieldOfStudy = "Software Engineering",
                    Location = "Test City, TC",
                    EndDate = DateTime.Now.AddYears(-5),
                    Gpa = 3.8,
                    Achievements = new List<string> { "Magna Cum Laude", "Dean's List" }
                }
            },
            Skills = new List<string> 
            { 
                "C#", ".NET Core", "ASP.NET", "Entity Framework", 
                "SQL Server", "PostgreSQL", "Docker", "Kubernetes",
                "Azure", "Git", "CI/CD", "Unit Testing"
            },
            Certifications = new List<CertificationDto>
            {
                new()
                {
                    Name = "Microsoft Certified: Azure Developer Associate",
                    IssuingOrganization = "Microsoft",
                    IssueDate = DateTime.Now.AddYears(-1),
                    CredentialId = "TEST-CERT-123"
                }
            },
            Projects = new List<ProjectDto>
            {
                new()
                {
                    Name = "Resume Generator API",
                    Description = "A comprehensive API for generating AI-powered resumes",
                    Technologies = new List<string> { "C#", ".NET 8", "Claude API", "OpenAI API" },
                    KeyFeatures = new List<string> 
                    { 
                        "AI-powered content generation",
                        "Multiple output formats",
                        "Template management"
                    }
                }
            },
            OutputFormat = OutputFormat.Html,
            IncludeAiReview = false, // Disable AI review for integration test
            CustomInstructions = "Focus on technical skills and achievements"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/resume/generate", resumeRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var jobResponse = await response.Content.ReadFromJsonAsync<ResumeGenerationResponseDto>();
        
        Assert.NotNull(jobResponse);
        Assert.NotEqual(Guid.Empty, jobResponse.JobId);
        Assert.Equal(JobStatus.Pending, jobResponse.Status);
        Assert.NotEmpty(jobResponse.Message);
    }

    [Fact]
    public async Task GenerateResume_InvalidTemplateId_ReturnsBadRequest()
    {
        // Arrange
        var resumeRequest = new ResumeGenerationRequestDto
        {
            TemplateId = Guid.NewGuid(), // Non-existent template
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "Test",
                LastName = "User"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/resume/generate", resumeRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetJobs_ReturnsPagedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/resume/jobs?pageNumber=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var pagedJobs = await response.Content.ReadFromJsonAsync<PagedResultDto<ResumeJobSummaryDto>>();
        
        Assert.NotNull(pagedJobs);
        Assert.True(pagedJobs.PageNumber >= 1);
        Assert.True(pagedJobs.PageSize >= 1);
        Assert.True(pagedJobs.TotalCount >= 0);
        Assert.NotNull(pagedJobs.Items);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var healthContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", healthContent);
    }

    [Fact]
    public async Task SwaggerEndpoint_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.EnsureSuccessStatusCode();
        var swaggerContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Resume Generator API", swaggerContent);
    }
}

/// <summary>
/// Custom web application factory for testing with service overrides
/// </summary>
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Override any services needed for testing
            services.AddLogging(logging => logging.AddDebug());
        });

        base.ConfigureWebHost(builder);
    }
}