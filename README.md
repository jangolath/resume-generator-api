# Resume Generator API

A powerful C# .NET 8 Web API that leverages AI services (Claude and OpenAI) to generate professional resumes from templates and personal data. The API supports multiple output formats and provides intelligent resume review capabilities.

## Features

- 🤖 **AI-Powered Generation**: Uses Claude API for intelligent resume content generation
- 📝 **AI Review & Suggestions**: Optional OpenAI-powered resume review with actionable feedback
- 📄 **Multiple Output Formats**: HTML, Google Docs, Markdown, Plain Text, and LaTeX
- 🎨 **Template Management**: Create, update, and manage resume templates
- 📊 **Job Tracking**: Comprehensive job status tracking and progress monitoring
- 🔄 **Asynchronous Processing**: Non-blocking resume generation with real-time updates
- 🏥 **Health Monitoring**: Built-in health checks and monitoring capabilities
- 🔒 **Rate Limiting**: Configurable rate limiting for API protection
- 📈 **Comprehensive Logging**: Structured logging with Serilog
- 🧪 **Fully Tested**: Extensive unit and integration test coverage

## Architecture

This application follows Clean Architecture principles with:

- **Controllers**: Handle HTTP requests and responses
- **Services**: Business logic and external API integrations
- **Data Layer**: Entity Framework Core with PostgreSQL
- **Models**: DTOs, Entities, and Enums for type safety
- **Middleware**: Cross-cutting concerns (logging, error handling, rate limiting)

## Tech Stack

- **.NET 8**: Latest LTS version of .NET
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: ORM with PostgreSQL support
- **Serilog**: Structured logging
- **xUnit**: Testing framework with Moq for mocking
- **Swagger/OpenAPI**: API documentation
- **Docker**: Containerization support

## Quick Start

### Prerequisites

- .NET 8 SDK
- PostgreSQL database
- Claude API key (from Anthropic)
- OpenAI API key (optional, for resume review)
- Google Service Account credentials (optional, for Google Docs integration)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/resume-generator-api.git
   cd resume-generator-api
   ```

2. **Set up PostgreSQL database**
   ```bash
   # Create database
   createdb resumegenerator
   
   # Create user (optional)
   psql -c "CREATE USER resumeuser WITH PASSWORD 'yourpassword';"
   psql -c "GRANT ALL PRIVILEGES ON DATABASE resumegenerator TO resumeuser;"
   ```

3. **Configure application settings**
   ```bash
   # Copy the example settings
   cp appsettings.json appsettings.Development.json
   
   # Edit the configuration file
   nano appsettings.Development.json
   ```

   Update the following settings:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=resumegenerator;Username=resumeuser;Password=yourpassword"
     },
     "ClaudeApi": {
       "ApiKey": "your-claude-api-key-here"
     },
     "OpenAI": {
       "ApiKey": "your-openai-api-key-here"
     }
   }
   ```

4. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

5. **Start the application**
   ```bash
   dotnet run
   ```

6. **Access the API**
   - API: `https://localhost:7001`
   - Swagger UI: `https://localhost:7001/swagger`
   - Health Check: `https://localhost:7001/health`

## Usage Examples

### Generate a Resume

```http
POST /api/resume/generate
Content-Type: application/json

{
  "templateId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "personalInfo": {
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "+1-555-0123",
    "professionalSummary": "Experienced software engineer with 5 years in full-stack development"
  },
  "experience": [
    {
      "jobTitle": "Senior Software Engineer",
      "company": "Tech Corp",
      "location": "San Francisco, CA",
      "startDate": "2020-01-15",
      "endDate": "2024-03-30",
      "description": "Led development of microservices architecture",
      "accomplishments": [
        "Reduced API response time by 40%",
        "Mentored 3 junior developers"
      ],
      "technologies": ["C#", ".NET", "Docker", "Kubernetes"]
    }
  ],
  "education": [
    {
      "institution": "University of Technology",
      "degree": "Bachelor of Science",
      "fieldOfStudy": "Computer Science",
      "endDate": "2019-05-15",
      "gpa": 3.8
    }
  ],
  "skills": ["C#", ".NET", "JavaScript", "React", "PostgreSQL", "Docker"],
  "outputFormat": "Html",
  "includeAiReview": true,
  "customInstructions": "Focus on technical leadership and team collaboration"
}
```

### Get Job Status

```http
GET /api/resume/job/{jobId}/status
```

### Get Job Result

```http
GET /api/resume/job/{jobId}/result
```

### List Templates

```http
GET /api/template
```

### Create a New Template

```http
POST /api/template
Content-Type: application/json

{
  "name": "Modern Professional",
  "description": "A clean, modern template for professionals",
  "content": "<html>...template content with {{placeholders}}...</html>",
  "format": "Html",
  "tags": ["professional", "modern", "clean"],
  "isPublic": true
}
```

## API Documentation

The complete API documentation is available via Swagger UI when running the application:
- **Development**: `https://localhost:7001/swagger`
- **Production**: `https://your-domain.com/swagger`

### Main Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/resume/generate` | POST | Generate a new resume |
| `/api/resume/job/{id}/status` | GET | Get job status |
| `/api/resume/job/{id}/result` | GET | Get job result |
| `/api/resume/jobs` | GET | List all jobs (paginated) |
| `/api/template` | GET | List all templates |
| `/api/template/{id}` | GET | Get specific template |
| `/api/template` | POST | Create new template |
| `/api/template/{id}` | PUT | Update template |
| `/api/template/{id}` | DELETE | Delete template |
| `/health` | GET | Health check endpoint |

## Configuration

### Environment Variables

For production deployment, use environment variables:

```bash
# Database
DATABASE_URL="Host=your-db-host;Database=resumegenerator;Username=user;Password=pass"

# AI APIs
CLAUDE_API_KEY="your-claude-api-key"
OPENAI_API_KEY="your-openai-api-key"
OPENAI_ORG_ID="your-openai-org-id"

# Google Docs (optional)
GOOGLE_SERVICE_ACCOUNT_JSON="{'type':'service_account',...}"
```

### Application Settings

Key configuration sections:

- **ClaudeApi**: Claude API configuration
- **OpenAI**: OpenAI API configuration
- **Application**: General application settings
- **RateLimit**: Rate limiting configuration
- **Serilog**: Logging configuration

## Deployment

### Digital Ocean App Platform

1. **Create a new app** in Digital Ocean App Platform
2. **Connect your GitHub repository**
3. **Configure environment variables** in the app settings
4. **Set up managed PostgreSQL database**
5. **Deploy the application**

### Docker Deployment

```bash
# Build the image
docker build -t resume-generator-api .

# Run with environment variables
docker run -d \
  -p 8080:8080 \
  -e DATABASE_URL="your-connection-string" \
  -e CLAUDE_API_KEY="your-claude-key" \
  -e OPENAI_API_KEY="your-openai-key" \
  resume-generator-api
```

### Docker Compose

```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - DATABASE_URL=Host=db;Database=resumegenerator;Username=postgres;Password=password
      - CLAUDE_API_KEY=your-claude-key
      - OPENAI_API_KEY=your-openai-key
    depends_on:
      - db

  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=resumegenerator
      - POSTGRES_PASSWORD=password
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
```

### Test Structure

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test complete workflows with in-memory database
- **API Tests**: Test HTTP endpoints end-to-end

## Monitoring and Observability

### Health Checks

The API includes comprehensive health checks:
- Database connectivity
- External API availability (Claude, OpenAI)
- Application health

### Logging

Structured logging with Serilog provides:
- Request/response logging
- Performance metrics
- Error tracking
- Business event logging

### Metrics

Key metrics to monitor:
- Response times
- Success/failure rates
- Queue lengths
- API token usage

## Security

- **Rate limiting** prevents abuse
- **Input validation** protects against malicious data
- **API key management** secures external service access
- **Error handling** prevents information disclosure
- **CORS configuration** controls cross-origin access

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Write tests for new features
- Update documentation for API changes
- Use meaningful commit messages
- Ensure all tests pass before submitting PR

## Roadmap

- [ ] **Enhanced Template Editor**: Visual template builder
- [ ] **Batch Processing**: Generate multiple resumes simultaneously
- [ ] **Resume Analytics**: Track resume performance metrics
- [ ] **Additional AI Providers**: Support for more AI services
- [ ] **Export to PDF**: Direct PDF generation capability
- [ ] **Resume Versioning**: Track and manage resume versions
- [ ] **Collaborative Features**: Team-based resume management

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Documentation**: See the `/docs` folder for detailed guides
- **Issues**: Report bugs and request features via GitHub Issues
- **Discussions**: Join the community discussions for questions and ideas

## Acknowledgments

- [Anthropic](https://anthropic.com) for the Claude API
- [OpenAI](https://openai.com) for the GPT models
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/) team
- [Entity Framework](https://docs.microsoft.com/en-us/ef/) team

---

**Built with ❤️ for the developer community**