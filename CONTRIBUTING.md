# Contributing to Resume Generator API

Thank you for your interest in contributing to the Resume Generator API! This document provides guidelines and information for contributors.

## 🎯 How to Contribute

We welcome contributions in the following areas:

- **Bug fixes** - Help us identify and fix issues
- **Feature enhancements** - Add new functionality or improve existing features
- **Documentation** - Improve our docs, examples, and guides
- **Testing** - Add test coverage or improve existing tests
- **Performance improvements** - Optimize code and database queries
- **Security enhancements** - Strengthen security measures
- **Code quality** - Refactoring and code cleanup

## 🚀 Getting Started

### Prerequisites

Before you begin, ensure you have:

- **.NET 8 SDK** installed
- **PostgreSQL** database (or Docker)
- **Git** for version control
- **Code editor** (Visual Studio, VS Code, or Rider)
- **API keys** for testing (Claude and OpenAI)

### Development Setup

1. **Fork and clone the repository**
   ```bash
   git clone https://github.com/yourusername/resume-generator-api.git
   cd resume-generator-api
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b bugfix/issue-description
   ```

3. **Set up development environment**
   ```bash
   # Copy environment template
   cp .env.template .env
   
   # Edit .env with your local settings
   nano .env
   ```

4. **Run the application**
   ```bash
   # Using Docker (recommended)
   docker-compose up -d
   
   # Or manually
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

5. **Verify setup**
   - Open browser to `http://localhost:7001`
   - Check Swagger UI is accessible
   - Run health check: `curl http://localhost:7001/health`

## 📝 Development Guidelines

### Code Style

We follow Microsoft's C# coding conventions with some specific preferences:

#### Naming Conventions
```csharp
// Use PascalCase for classes, methods, properties
public class ResumeGenerationService
{
    public async Task<ResumeDto> GenerateResumeAsync()
}

// Use camelCase for local variables and parameters
public void ProcessJob(Guid jobId, string templateContent)
{
    var startTime = DateTime.UtcNow;
}

// Use SCREAMING_SNAKE_CASE for constants
private const int MAX_RETRY_ATTEMPTS = 3;
```

#### File Organization
```csharp
// Order of class members:
// 1. Constants
// 2. Fields
// 3. Constructors
// 4. Properties
// 5. Public methods
// 6. Private methods

public class ExampleService
{
    private const int DEFAULT_TIMEOUT = 30;
    
    private readonly ILogger<ExampleService> _logger;
    
    public ExampleService(ILogger<ExampleService> logger)
    {
        _logger = logger;
    }
    
    public string PublicProperty { get; set; }
    
    public async Task<string> PublicMethodAsync()
    {
        return await PrivateMethodAsync();
    }
    
    private async Task<string> PrivateMethodAsync()
    {
        // Implementation
    }
}
```

#### Comments and Documentation
```csharp
/// <summary>
/// Generates a resume using AI services and template processing
/// </summary>
/// <param name="request">The resume generation request containing personal data</param>
/// <returns>A task containing the generated resume response</returns>
/// <exception cref="ArgumentException">Thrown when template ID is invalid</exception>
public async Task<ResumeGenerationResponseDto> GenerateResumeAsync(
    ResumeGenerationRequestDto request)
{
    // Validate input parameters
    if (request.TemplateId == Guid.Empty)
    {
        throw new ArgumentException("Template ID cannot be empty", nameof(request));
    }
    
    // TODO: Add caching for frequently used templates
    
    return result;
}
```

### Architecture Patterns

#### Dependency Injection
Always use constructor injection and interfaces:
```csharp
public class ResumeController : ControllerBase
{
    private readonly IResumeGenerationService _resumeService;
    private readonly ILogger<ResumeController> _logger;

    public ResumeController(
        IResumeGenerationService resumeService,
        ILogger<ResumeController> logger)
    {
        _resumeService = resumeService;
        _logger = logger;
    }
}
```

#### Error Handling
Use consistent error handling patterns:
```csharp
public async Task<ActionResult<ResumeDto>> GenerateResume(ResumeRequestDto request)
{
    try
    {
        var result = await _resumeService.GenerateResumeAsync(request);
        return Ok(result);
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "Invalid request parameters");
        return BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during resume generation");
        return StatusCode(500, new { error = "An unexpected error occurred" });
    }
}
```

#### Logging
Use structured logging with Serilog:
```csharp
// Good logging practices
_logger.LogInformation("Starting resume generation for job {JobId} with template {TemplateId}", 
    jobId, templateId);

_logger.LogWarning("Template {TemplateId} not found for user {UserId}", 
    templateId, userId);

_logger.LogError(ex, "Failed to process job {JobId} after {ElapsedMs}ms", 
    jobId, stopwatch.ElapsedMilliseconds);
```

### Database Guidelines

#### Entity Framework
- Use async methods for all database operations
- Include proper navigation properties
- Add appropriate indexes for performance
- Use meaningful constraint names

```csharp
// Good entity design
[Table("resume_jobs")]
public class ResumeJob
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("template_id")]
    public Guid TemplateId { get; set; }

    [ForeignKey("TemplateId")]
    public virtual ResumeTemplate Template { get; set; } = null!;
}

// Good repository pattern
public async Task<ResumeJob?> GetJobByIdAsync(Guid jobId)
{
    return await _context.ResumeJobs
        .Include(j => j.Template)
        .FirstOrDefaultAsync(j => j.Id == jobId);
}
```

#### Migrations
- Create descriptive migration names
- Review generated migrations before applying
- Test migrations on development data

```bash
# Good migration names
dotnet ef migrations add AddJobStatusIndex
dotnet ef migrations add UpdateTemplateSchemaForVersioning
dotnet ef migrations add AddUserAuthenticationTables

# Bad migration names
dotnet ef migrations add Update
dotnet ef migrations add Fix
```

## 🧪 Testing Guidelines

### Unit Tests
Write comprehensive unit tests for all business logic:

```csharp
[Fact]
public async Task GenerateResume_ValidRequest_ReturnsSuccessResponse()
{
    // Arrange
    var request = CreateValidRequest();
    var expectedResponse = new ResumeGenerationResponseDto { JobId = Guid.NewGuid() };
    
    _mockService.Setup(s => s.GenerateResumeAsync(It.IsAny<ResumeGenerationRequestDto>()))
               .ReturnsAsync(expectedResponse);

    // Act
    var result = await _controller.GenerateResume(request);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result.Result);
    var response = Assert.IsType<ResumeGenerationResponseDto>(okResult.Value);
    Assert.Equal(expectedResponse.JobId, response.JobId);
}
```

### Integration Tests
Test complete workflows with realistic data:

```csharp
[Fact]
public async Task ResumeGeneration_EndToEnd_CompletesSuccessfully()
{
    // Arrange
    var request = CreateCompleteResumeRequest();

    // Act
    var generateResponse = await _client.PostAsJsonAsync("/api/resume/generate", request);
    generateResponse.EnsureSuccessStatusCode();
    
    var jobResponse = await generateResponse.Content.ReadFromJsonAsync<ResumeGenerationResponseDto>();
    
    // Poll for completion
    var statusResponse = await PollForCompletion(jobResponse.JobId);
    
    // Assert
    Assert.Equal(JobStatus.Completed, statusResponse.Status);
}
```

### Test Data
Use the builder pattern for test data:

```csharp
public class ResumeRequestBuilder
{
    private ResumeGenerationRequestDto _request = new();

    public ResumeRequestBuilder WithTemplate(Guid templateId)
    {
        _request.TemplateId = templateId;
        return this;
    }

    public ResumeRequestBuilder WithPersonalInfo(string firstName, string lastName)
    {
        _request.PersonalInfo = new PersonalInfoDto
        {
            FirstName = firstName,
            LastName = lastName
        };
        return this;
    }

    public ResumeGenerationRequestDto Build() => _request;
}

// Usage in tests
var request = new ResumeRequestBuilder()
    .WithTemplate(templateId)
    .WithPersonalInfo("John", "Doe")
    .Build();
```

## 📦 Pull Request Process

### Before Submitting

1. **Run all tests**
   ```bash
   dotnet test
   ```

2. **Check code formatting**
   ```bash
   dotnet format
   ```

3. **Run security analysis**
   ```bash
   dotnet list package --vulnerable
   ```

4. **Update documentation** if needed

### Pull Request Guidelines

#### Title Format
```
[Type]: Brief description

Examples:
feat: Add resume template versioning
fix: Resolve database connection timeout issue
docs: Update API documentation for new endpoints
test: Add integration tests for resume generation
refactor: Simplify job processing logic
```

#### Description Template
```markdown
## Description
Brief description of what this PR does.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed

## Checklist
- [ ] My code follows the style guidelines of this project
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes

## Screenshots (if applicable)
Add screenshots here if your changes affect the UI or API responses.
```

### Review Process

1. **Automated checks** must pass (CI/CD pipeline)
2. **Code review** by at least one maintainer
3. **Testing** verification in review environment
4. **Documentation** updates reviewed
5. **Approval** and merge by maintainer

## 🐛 Reporting Issues

### Bug Reports

Use the issue template and include:

- **Environment details** (OS, .NET version, database version)
- **Steps to reproduce** the issue
- **Expected vs actual behavior**
- **Error messages** and stack traces
- **Code samples** if applicable

### Feature Requests

For new features, please include:

- **Use case description** - Why is this needed?
- **Proposed solution** - How should it work?
- **Alternatives considered** - What other approaches were considered?
- **Additional context** - Screenshots, mockups, or examples

## 📋 Development Workflow

### Branch Naming
```
feature/add-template-versioning
bugfix/fix-job-timeout-issue
hotfix/critical-security-patch
docs/update-api-documentation
test/add-integration-tests
```

### Commit Message Format
```
type(scope): description

feat(api): add resume template versioning
fix(db): resolve connection timeout in job processing
docs(readme): update deployment instructions
test(unit): add tests for resume validation
refactor(service): simplify AI service integration
```

### Release Process

1. **Version bumping** follows semantic versioning
2. **Changelog** is updated with notable changes
3. **Documentation** is updated for new features
4. **Migration guide** for breaking changes
5. **Release notes** published with each release

## 🎭 Code of Conduct

### Our Standards

- Use welcoming and inclusive language
- Be respectful of differing viewpoints and experiences
- Gracefully accept constructive criticism
- Focus on what is best for the community
- Show empathy towards other community members

### Enforcement

Instances of abusive, harassing, or otherwise unacceptable behavior may be reported to the project maintainers. All complaints will be reviewed and investigated promptly and fairly.

## 🔧 Development Tools

### Recommended Extensions (VS Code)

- **C# Dev Kit** - Microsoft
- **GitLens** - GitKraken
- **REST Client** - Huachao Mao
- **Docker** - Microsoft
- **PostgreSQL** - Chris Kolkman

### Useful Commands

```bash
# Database commands
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef database drop --force

# Testing commands
dotnet test --collect:"XPlat Code Coverage"
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration

# Code analysis
dotnet format
dotnet build --verbosity normal
dotnet publish -c Release
```

## 📞 Getting Help

- **GitHub Issues** - For bugs and feature requests
- **GitHub Discussions** - For questions and general discussion
- **Documentation** - Check the `/docs` folder
- **Stack Overflow** - Tag questions with `resume-generator-api`

## 🙏 Recognition

Contributors will be recognized in:

- **README.md** contributors section
- **Release notes** for significant contributions
- **Contributors page** on project website

Thank you for contributing to Resume Generator API! 🚀