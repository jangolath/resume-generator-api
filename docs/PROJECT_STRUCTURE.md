# Resume Generator API - Project Structure

This document provides an overview of the complete project structure and the purpose of each file and directory.

## 📁 Root Directory

```
ResumeGenerator.API/
├── 📄 Program.cs                              # Application entry point and service configuration
├── 📄 ResumeGenerator.API.csproj              # Project file with dependencies
├── 📄 Dockerfile                              # Docker container configuration
├── 📄 docker-compose.yml                      # Multi-container Docker setup
├── 📄 .env.template                           # Environment variables template
├── 📄 README.md                               # Project documentation
├── 📄 PROJECT_STRUCTURE.md                    # This file - project overview
├── 📁 Controllers/                            # API controllers
├── 📁 Services/                               # Business logic and external integrations
├── 📁 Models/                                 # Data models, DTOs, and entities
├── 📁 Data/                                   # Database context and configuration
├── 📁 Configuration/                          # Application settings classes
├── 📁 Middleware/                             # Custom middleware components
├── 📁 Extensions/                             # Extension methods for service configuration
├── 📁 scripts/                                # Database and deployment scripts
├── 📁 Tests/                                  # Unit and integration tests
├── 📁 .github/                                # GitHub Actions CI/CD workflows
└── 📁 logs/                                   # Application logs (created at runtime)
```

## 🎯 Core Application Files

### **Program.cs**
- Application entry point
- Service registration and dependency injection
- Middleware pipeline configuration
- Uses extension methods for clean configuration

### **ResumeGenerator.API.csproj**
- Project dependencies and packages
- .NET 8 targeting
- NuGet package references for EF Core, Serilog, AI APIs, etc.

## 🎮 Controllers (API Endpoints)

### **Controllers/ResumeController.cs**
- Primary API endpoints for resume generation
- Job status and result retrieval
- Asynchronous processing with progress tracking
- Full CRUD operations for resume generation jobs

### **Controllers/TemplateController.cs**
- Template management endpoints
- CRUD operations for resume templates
- Google Docs import functionality
- Template validation and versioning

## 🧠 Services (Business Logic)

### **Services/Interfaces/IServices.cs**
- Service contracts and interfaces
- Defines all business logic operations
- Dependency injection abstractions

### **Services/Implementation/**

#### **ResumeGenerationService.cs**
- Main orchestration service
- Coordinates AI services and template processing
- Handles the complete resume generation workflow

#### **ClaudeService.cs**
- Anthropic Claude API integration
- Resume content generation using AI
- Token usage tracking and error handling

#### **OpenAIService.cs**
- OpenAI API integration
- Resume review and suggestions
- Keyword analysis and feedback generation

#### **ResumeJobService.cs**
- Job lifecycle management
- Status tracking and progress monitoring
- Database operations for jobs

#### **ResumeTemplateService.cs**
- Template CRUD operations
- Usage statistics tracking
- Template validation and management

### **Services/BackgroundServices/**

#### **ResumeJobProcessorService.cs**
- Background job processing
- Asynchronous resume generation
- Concurrent job handling with limits
- Automatic retry and error handling

#### **JobCleanupService.cs**
- Automated cleanup of old jobs
- Database maintenance
- Configurable retention policies

## 📊 Data Layer

### **Data/ResumeGeneratorContext.cs**
- Entity Framework database context
- Table relationships and constraints
- Seed data for default templates
- Database configuration and indexing

### **Models/Entities/ResumeEntities.cs**
- Database entity definitions
- Table mappings and relationships
- Data annotations and constraints

### **Models/DTOs/**

#### **RequestDTOs.cs**
- Request models for API endpoints
- Input validation attributes
- Data transfer objects for client requests

#### **ResponseDTOs.cs**
- Response models for API endpoints
- Formatted output for clients
- Pagination and result wrapping

### **Models/Enums/Enums.cs**
- Enumeration definitions
- Job status, output formats, processing steps
- Industry categories and experience levels

### **Models/Validation/CustomValidationAttributes.cs**
- Custom validation logic
- Phone number, date range, GPA validation
- Content filtering and business rule validation

## ⚙️ Configuration

### **Configuration/ApiSettings.cs**
- Strongly-typed configuration classes
- API keys and external service settings
- Application behavior configuration

### **Extensions/ServiceCollectionExtensions.cs**
- Clean service registration
- Modular configuration setup
- Health checks and monitoring
- Security and CORS configuration

## 🛠️ Infrastructure

### **Middleware/**

#### **ExceptionHandlingMiddleware.cs**
- Global exception handling
- Standardized error responses
- Security-focused error messages

#### **RequestLoggingMiddleware.cs**
- Request/response logging
- Performance monitoring
- Request tracing and correlation

#### **RateLimitingMiddleware.cs**
- API rate limiting
- IP-based request throttling
- Configurable limits and whitelisting

## 📋 Configuration Files

### **appsettings.json**
- Default application settings
- Database connection strings
- API configuration and logging

### **appsettings.Development.json**
- Development environment overrides
- Debug logging and relaxed security
- Local database and API settings

### **appsettings.Production.json**
- Production environment configuration
- Environment variable references
- Optimized logging and security settings

## 🧪 Testing

### **Tests/ResumeGenerator.API.Tests/**

#### **UnitTests/ResumeControllerTests.cs**
- Controller unit tests
- Mocked dependencies
- Input validation and error handling tests

#### **IntegrationTests/ResumeGenerationIntegrationTests.cs**
- End-to-end API testing
- In-memory database testing
- Full workflow validation

#### **ResumeGenerator.API.Tests.csproj**
- Test project configuration
- Testing framework dependencies
- Test data and fixtures

## 🐳 Deployment

### **Dockerfile**
- Multi-stage Docker build
- Security optimizations
- Health checks and monitoring

### **docker-compose.yml**
- Complete development environment
- PostgreSQL, Redis, and pgAdmin
- Environment variable configuration
- Service networking and volumes

### **.github/workflows/ci-cd.yml**
- Automated CI/CD pipeline
- Testing, security scanning, and deployment
- Multi-environment support (staging/production)
- Docker image building and publishing

## 📝 Database

### **scripts/init-db.sql**
- Database initialization script
- User creation and permissions
- Performance indexes and views
- Sample data and maintenance functions

## 🔧 Development Tools

### **.env.template**
- Environment variable template
- API key placeholders
- Configuration examples and documentation
- Security best practices

## 📖 Key Features Implemented

### ✅ **Core Functionality**
- AI-powered resume generation using Claude
- Intelligent resume review with OpenAI
- Multiple output formats (HTML, PDF, Markdown, LaTeX)
- Template management with Google Docs import
- Asynchronous job processing with progress tracking

### ✅ **Architecture & Design**
- Clean Architecture principles
- Dependency injection throughout
- Repository pattern with Entity Framework
- Service layer abstraction
- Comprehensive error handling

### ✅ **Quality & Testing**
- Unit tests with Moq
- Integration tests with in-memory database
- Code coverage reporting
- Security vulnerability scanning

### ✅ **DevOps & Deployment**
- Docker containerization
- GitHub Actions CI/CD
- Health checks and monitoring
- Structured logging with Serilog
- Rate limiting and security middleware

### ✅ **Enterprise Features**
- Background job processing
- Database migrations
- Configuration management
- API documentation with Swagger
- Comprehensive error handling

## 🚀 Getting Started

1. **Prerequisites**: .NET 8 SDK, PostgreSQL, API keys
2. **Clone repository**: `git clone <repo-url>`
3. **Configure settings**: Copy `.env.template` to `.env` and fill in values
4. **Run with Docker**: `docker-compose up -d`
5. **Access API**: Navigate to `http://localhost:8080`
6. **View documentation**: Open Swagger UI at root URL

## 📈 Scalability Considerations

- **Horizontal scaling**: Stateless design with external job queue
- **Database optimization**: Indexed queries and connection pooling
- **Caching**: Memory caching for templates and frequently accessed data
- **Background processing**: Separate worker instances for job processing
- **Monitoring**: Health checks and structured logging for observability

## 🔒 Security Features

- **Input validation**: Comprehensive validation attributes
- **Rate limiting**: IP-based request throttling
- **Security headers**: XSS, CSRF, and clickjacking protection
- **Error handling**: No sensitive information disclosure
- **Environment separation**: Different configs for different environments

This structure provides a robust foundation for an enterprise-grade resume generation API that can scale and be maintained effectively.