using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ResumeGenerator.API.Controllers;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Enums;
using ResumeGenerator.API.Services.Interfaces;
using Xunit;

namespace ResumeGenerator.API.Tests.UnitTests;

/// <summary>
/// Unit tests for ResumeController
/// </summary>
public class ResumeControllerTests
{
    private readonly Mock<IResumeGenerationService> _mockResumeGenerationService;
    private readonly Mock<IResumeJobService> _mockResumeJobService;
    private readonly Mock<ILogger<ResumeController>> _mockLogger;
    private readonly ResumeController _controller;

    public ResumeControllerTests()
    {
        _mockResumeGenerationService = new Mock<IResumeGenerationService>();
        _mockResumeJobService = new Mock<IResumeJobService>();
        _mockLogger = new Mock<ILogger<ResumeController>>();
        
        _controller = new ResumeController(
            _mockResumeGenerationService.Object,
            _mockResumeJobService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateResume_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = CreateValidResumeRequest();
        var expectedResponse = new ResumeGenerationResponseDto
        {
            JobId = Guid.NewGuid(),
            Status = JobStatus.Pending,
            Message = "Resume generation started"
        };

        _mockResumeGenerationService
            .Setup(s => s.GenerateResumeAsync(It.IsAny<ResumeGenerationRequestDto>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GenerateResume(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ResumeGenerationResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse.JobId, response.JobId);
        Assert.Equal(expectedResponse.Status, response.Status);
    }

    [Fact]
    public async Task GenerateResume_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidResumeRequest();
        _mockResumeGenerationService
            .Setup(s => s.GenerateResumeAsync(It.IsAny<ResumeGenerationRequestDto>()))
            .ThrowsAsync(new ArgumentException("Invalid template ID"));

        // Act
        var result = await _controller.GenerateResume(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task GenerateResume_ServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var request = CreateValidResumeRequest();
        _mockResumeGenerationService
            .Setup(s => s.GenerateResumeAsync(It.IsAny<ResumeGenerationRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act
        var result = await _controller.GenerateResume(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetJobStatus_ExistingJob_ReturnsJobStatus()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var expectedStatus = new ResumeJobStatusDto
        {
            JobId = jobId,
            Status = JobStatus.InProgress,
            ProgressPercentage = 50,
            CurrentStep = "Processing with AI"
        };

        _mockResumeJobService
            .Setup(s => s.GetJobStatusAsync(jobId))
            .ReturnsAsync(expectedStatus);

        // Act
        var result = await _controller.GetJobStatus(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<ResumeJobStatusDto>(okResult.Value);
        Assert.Equal(jobId, status.JobId);
        Assert.Equal(JobStatus.InProgress, status.Status);
    }

    [Fact]
    public async Task GetJobStatus_NonExistentJob_ReturnsNotFound()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _mockResumeJobService
            .Setup(s => s.GetJobStatusAsync(jobId))
            .ReturnsAsync((ResumeJobStatusDto?)null);

        // Act
        var result = await _controller.GetJobStatus(jobId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetJobResult_CompletedJob_ReturnsContent()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var expectedContent = new ResumeContentDto
        {
            GeneratedContent = "<html>Resume content</html>",
            Format = OutputFormat.Html,
            Metadata = new GenerationMetadataDto
            {
                TemplateId = Guid.NewGuid(),
                GeneratedAt = DateTime.UtcNow,
                ProcessingTimeMs = 5000
            }
        };

        _mockResumeJobService
            .Setup(s => s.GetJobResultAsync(jobId))
            .ReturnsAsync(expectedContent);

        // Act
        var result = await _controller.GetJobResult(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var content = Assert.IsType<ResumeContentDto>(okResult.Value);
        Assert.Equal(expectedContent.GeneratedContent, content.GeneratedContent);
        Assert.Equal(expectedContent.Format, content.Format);
    }

    [Fact]
    public async Task GetJobs_ValidRequest_ReturnsPagedResults()
    {
        // Arrange
        var expectedJobs = new PagedResultDto<ResumeJobSummaryDto>
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 2,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false,
            Items = new List<ResumeJobSummaryDto>
            {
                new() { JobId = Guid.NewGuid(), Status = JobStatus.Completed, PersonName = "John Doe" },
                new() { JobId = Guid.NewGuid(), Status = JobStatus.InProgress, PersonName = "Jane Smith" }
            }
        };

        _mockResumeJobService
            .Setup(s => s.GetJobsAsync(null, 1, 10))
            .ReturnsAsync(expectedJobs);

        // Act
        var result = await _controller.GetJobs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var jobs = Assert.IsType<PagedResultDto<ResumeJobSummaryDto>>(okResult.Value);
        Assert.Equal(2, jobs.Items.Count);
        Assert.Equal(2, jobs.TotalCount);
    }

    [Fact]
    public async Task CancelJob_ExistingJob_ReturnsOk()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _mockResumeJobService
            .Setup(s => s.CancelJobAsync(jobId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelJob(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CancelJob_NonExistentJob_ReturnsNotFound()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _mockResumeJobService
            .Setup(s => s.CancelJobAsync(jobId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CancelJob(jobId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static ResumeGenerationRequestDto CreateValidResumeRequest()
    {
        return new ResumeGenerationRequestDto
        {
            TemplateId = Guid.NewGuid(),
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "+1-555-0123"
            },
            Experience = new List<ExperienceDto>
            {
                new()
                {
                    JobTitle = "Software Engineer",
                    Company = "Tech Corp",
                    StartDate = DateTime.Now.AddYears(-2),
                    EndDate = DateTime.Now,
                    Description = "Developed web applications"
                }
            },
            Education = new List<EducationDto>
            {
                new()
                {
                    Institution = "University of Technology",
                    Degree = "Bachelor of Science",
                    FieldOfStudy = "Computer Science",
                    EndDate = DateTime.Now.AddYears(-3)
                }
            },
            Skills = new List<string> { "C#", ".NET", "JavaScript", "React" },
            OutputFormat = OutputFormat.Html,
            IncludeAiReview = true
        };
    }
}