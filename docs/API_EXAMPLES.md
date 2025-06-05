# API Request/Response Examples

This document provides comprehensive examples of API requests and responses for the Resume Generator API.

## 🔗 Base URL

- **Development**: `http://localhost:7001`
- **Production**: `https://your-domain.com`

## 📋 Table of Contents

1. [Resume Generation](#resume-generation)
2. [Template Management](#template-management)
3. [Job Management](#job-management)
4. [Error Responses](#error-responses)
5. [Pagination](#pagination)

---

## 🎯 Resume Generation

### Generate Resume

Generate a new resume using AI services.

**Endpoint:** `POST /api/resume/generate`

#### Request Example

```http
POST /api/resume/generate
Content-Type: application/json

{
  "templateId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "personalInfo": {
    "firstName": "Sarah",
    "lastName": "Johnson",
    "email": "sarah.johnson@email.com",
    "phone": "+1-555-0198",
    "address": "123 Tech Street, San Francisco, CA 94102",
    "linkedInUrl": "https://linkedin.com/in/sarahjohnson",
    "gitHubUrl": "https://github.com/sarahjohnson",
    "personalWebsite": "https://sarahjohnson.dev",
    "professionalSummary": "Experienced full-stack developer with 5+ years of expertise in building scalable web applications using modern technologies. Passionate about clean code, user experience, and continuous learning."
  },
  "experience": [
    {
      "jobTitle": "Senior Software Engineer",
      "company": "TechCorp Solutions",
      "location": "San Francisco, CA",
      "startDate": "2021-03-15T00:00:00Z",
      "endDate": "2024-01-30T00:00:00Z",
      "isCurrentPosition": false,
      "description": "Led development of microservices architecture serving 1M+ daily active users",
      "accomplishments": [
        "Reduced API response time by 45% through database optimization and caching strategies",
        "Mentored 4 junior developers and established code review best practices",
        "Designed and implemented real-time notification system handling 500K+ messages daily",
        "Led migration from monolith to microservices, improving deployment frequency by 300%"
      ],
      "technologies": [
        "C#", ".NET Core", "React", "TypeScript", "PostgreSQL", 
        "Docker", "Kubernetes", "Azure", "Redis", "RabbitMQ"
      ]
    },
    {
      "jobTitle": "Software Engineer",
      "company": "StartupInc",
      "location": "San Francisco, CA",
      "startDate": "2019-06-01T00:00:00Z",
      "endDate": "2021-03-10T00:00:00Z",
      "isCurrentPosition": false,
      "description": "Full-stack development for early-stage fintech startup",
      "accomplishments": [
        "Built MVP from scratch that acquired 10K+ users in first 6 months",
        "Implemented secure payment processing with 99.9% uptime",
        "Developed responsive web application supporting mobile and desktop users"
      ],
      "technologies": [
        "JavaScript", "Node.js", "Express", "MongoDB", "React", "AWS", "Stripe API"
      ]
    }
  ],
  "education": [
    {
      "institution": "University of California, Berkeley",
      "degree": "Bachelor of Science",
      "fieldOfStudy": "Computer Science",
      "location": "Berkeley, CA",
      "startDate": "2015-08-15T00:00:00Z",
      "endDate": "2019-05-15T00:00:00Z",
      "gpa": 3.8,
      "achievements": [
        "Magna Cum Laude",
        "Dean's List (6 semesters)",
        "President of Women in Computer Science Club"
      ]
    }
  ],
  "skills": [
    "C#", ".NET Core", "ASP.NET", "Entity Framework",
    "JavaScript", "TypeScript", "React", "Node.js",
    "SQL Server", "PostgreSQL", "MongoDB", "Redis",
    "Docker", "Kubernetes", "Azure", "AWS",
    "Git", "CI/CD", "Agile", "Scrum", "TDD"
  ],
  "certifications": [
    {
      "name": "Microsoft Certified: Azure Developer Associate",
      "issuingOrganization": "Microsoft",
      "issueDate": "2023-06-15T00:00:00Z",
      "expirationDate": "2025-06-15T00:00:00Z",
      "credentialId": "AZ-204-2023-06-15",
      "verificationUrl": "https://learn.microsoft.com/api/credentials/share/en-us/SarahJohnson/ABC123"
    },
    {
      "name": "AWS Certified Solutions Architect",
      "issuingOrganization": "Amazon Web Services",
      "issueDate": "2022-09-20T00:00:00Z",
      "expirationDate": "2025-09-20T00:00:00Z",
      "credentialId": "AWS-SAA-2022-09-20",
      "verificationUrl": "https://aws.amazon.com/verification"
    }
  ],
  "projects": [
    {
      "name": "Personal Finance Tracker",
      "description": "Full-stack web application for tracking personal expenses with real-time analytics and budget planning features",
      "projectUrl": "https://financetracker.sarahjohnson.dev",
      "gitHubUrl": "https://github.com/sarahjohnson/finance-tracker",
      "startDate": "2023-01-01T00:00:00Z",
      "endDate": "2023-04-15T00:00:00Z",
      "technologies": [
        "React", "TypeScript", "Node.js", "Express", "PostgreSQL", "Chart.js"
      ],
      "keyFeatures": [
        "Real-time expense tracking with categorization",
        "Interactive charts and spending analytics",
        "Budget planning with alerts and notifications",
        "Secure authentication and data encryption"
      ]
    },
    {
      "name": "AI-Powered Code Review Bot",
      "description": "GitHub bot that automatically reviews pull requests and provides intelligent feedback using machine learning",
      "gitHubUrl": "https://github.com/sarahjohnson/code-review-bot",
      "startDate": "2023-08-01T00:00:00Z",
      "endDate": "2023-11-30T00:00:00Z",
      "technologies": [
        "Python", "GitHub API", "OpenAI API", "Docker", "GitHub Actions"
      ],
      "keyFeatures": [
        "Automated code quality analysis",
        "Security vulnerability detection",
        "Performance optimization suggestions",
        "Integration with popular development workflows"
      ]
    }
  ],
  "additionalSections": {
    "languages": ["English (Native)", "Spanish (Conversational)", "French (Basic)"],
    "volunteering": [
      "Code mentor at Girls Who Code (2022-Present)",
      "Technical workshop instructor at local community center"
    ],
    "interests": ["Open source contribution", "Technical blogging", "Rock climbing", "Photography"]
  },
  "outputFormat": "Html",
  "includeAiReview": true,
  "customInstructions": "Emphasize technical leadership experience and focus on quantifiable achievements. Use a modern, professional tone suitable for senior engineering roles at tech companies."
}
```

#### Response Example

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "jobId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "Pending",
  "estimatedCompletion": "2024-06-04T15:10:00Z",
  "message": "Resume generation job has been queued for processing",
  "content": null
}
```

---

## 📄 Template Management

### Get All Templates

Retrieve all available resume templates.

**Endpoint:** `GET /api/template`

#### Request Example

```http
GET /api/template
Accept: application/json
```

#### Response Example

```http
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "id": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
    "name": "Modern Professional",
    "description": "A clean, modern template suitable for most professional roles",
    "content": "<!DOCTYPE html><html>...</html>",
    "format": "Html",
    "tags": ["professional", "modern", "clean"],
    "isPublic": true,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-05-20T14:45:00Z",
    "usageCount": 1247
  },
  {
    "id": "b2c3d4e5-f6g7-8901-2345-678901bcdefg",
    "name": "Creative Portfolio",
    "description": "A creative template for designers and creative professionals",
    "content": "<!DOCTYPE html><html>...</html>",
    "format": "Html",
    "tags": ["creative", "portfolio", "design"],
    "isPublic": true,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-05-18T09:20:00Z",
    "usageCount": 892
  }
]
```

### Get Template by ID

Retrieve a specific template by its ID.

**Endpoint:** `GET /api/template/{id}`

#### Request Example

```http
GET /api/template/a1b2c3d4-e5f6-7890-1234-567890abcdef
Accept: application/json
```

#### Response Example

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "name": "Modern Professional",
  "description": "A clean, modern template suitable for most professional roles",
  "content": "<!DOCTYPE html>\n<html>\n<head>\n    <meta charset='UTF-8'>\n    <title>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}} - Resume</title>\n    <style>\n        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 40px; background: #f8f9fa; }\n        .container { max-width: 800px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 20px rgba(0,0,0,0.1); }\n        /* ... more CSS ... */\n    </style>\n</head>\n<body>\n    <div class='container'>\n        <div class='header'>\n            <div class='name'>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}}</div>\n            <div class='contact'>{{PersonalInfo.Email}} • {{PersonalInfo.Phone}}</div>\n        </div>\n        <!-- ... more HTML template ... -->\n    </div>\n</body>\n</html>",
  "format": "Html",
  "tags": ["professional", "modern", "clean"],
  "isPublic": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-05-20T14:45:00Z",
  "usageCount": 1247
}
```

### Create New Template

Create a new resume template.

**Endpoint:** `POST /api/template`

#### Request Example

```http
POST /api/template
Content-Type: application/json

{
  "name": "Executive Leadership",
  "description": "Professional template designed for C-level executives and senior leadership roles",
  "content": "<!DOCTYPE html>\n<html>\n<head>\n    <title>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}} - Executive Resume</title>\n    <style>\n        body { font-family: 'Times New Roman', serif; margin: 0; padding: 30px; }\n        .header { text-align: center; border-bottom: 2px solid #000; padding-bottom: 20px; }\n        .name { font-size: 2.2em; font-weight: bold; margin-bottom: 10px; }\n        /* ... executive styling ... */\n    </style>\n</head>\n<body>\n    <div class='header'>\n        <div class='name'>{{PersonalInfo.FirstName}} {{PersonalInfo.LastName}}</div>\n        <div class='title'>Executive Leader</div>\n    </div>\n    <!-- ... executive template content ... -->\n</body>\n</html>",
  "format": "Html",
  "tags": ["executive", "leadership", "professional", "senior"],
  "isPublic": true
}
```

#### Response Example

```http
HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/template/c3d4e5f6-g7h8-9012-3456-789012cdefgh

{
  "id": "c3d4e5f6-g7h8-9012-3456-789012cdefgh",
  "name": "Executive Leadership",
  "description": "Professional template designed for C-level executives and senior leadership roles",
  "content": "<!DOCTYPE html>...",
  "format": "Html",
  "tags": ["executive", "leadership", "professional", "senior"],
  "isPublic": true,
  "createdAt": "2024-06-04T14:30:00Z",
  "updatedAt": "2024-06-04T14:30:00Z",
  "usageCount": 0
}
```

---

## 📊 Job Management

### Get Job Status

Check the status of a resume generation job.

**Endpoint:** `GET /api/resume/job/{jobId}/status`

#### Request Example

```http
GET /api/resume/job/f47ac10b-58cc-4372-a567-0e02b2c3d479/status
Accept: application/json
```

#### Response Examples

**Pending Job:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "jobId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "Pending",
  "progressPercentage": 0,
  "currentStep": "Queued for processing",
  "createdAt": "2024-06-04T14:05:00Z",
  "updatedAt": "2024-06-04T14:05:00Z",
  "estimatedCompletion": "2024-06-04T14:10:00Z",
  "errorMessage": null
}
```

**In Progress Job:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "jobId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "InProgress",
  "progressPercentage": 60,
  "currentStep": "Reviewing resume with OpenAI",
  "createdAt": "2024-06-04T14:05:00Z",
  "updatedAt": "2024-06-04T14:07:30Z",
  "estimatedCompletion": "2024-06-04T14:09:00Z",
  "errorMessage": null
}
```

**Completed Job:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "jobId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "Completed",
  "progressPercentage": 100,
  "currentStep": "Resume generation completed successfully",
  "createdAt": "2024-06-04T14:05:00Z",
  "updatedAt": "2024-06-04T14:08:45Z",
  "estimatedCompletion": null,
  "errorMessage": null
}
```

### Get Job Result

Retrieve the generated resume content for a completed job.

**Endpoint:** `GET /api/resume/job/{jobId}/result`

#### Request Example

```http
GET /api/resume/job/f47ac10b-58cc-4372-a567-0e02b2c3d479/result
Accept: application/json
```

#### Response Example

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "generatedContent": "<!DOCTYPE html>\n<html>\n<head>\n    <meta charset='UTF-8'>\n    <title>Sarah Johnson - Resume</title>\n    <style>\n        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 40px; background: #f8f9fa; }\n        .container { max-width: 800px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 20px rgba(0,0,0,0.1); }\n        .header { text-align: center; border-bottom: 3px solid #007acc; padding-bottom: 20px; margin-bottom: 30px; }\n        .name { font-size: 2.5em; font-weight: bold; color: #333; margin-bottom: 10px; }\n        .contact { color: #666; font-size: 1.1em; }\n        /* ... more generated CSS ... */\n    </style>\n</head>\n<body>\n    <div class='container'>\n        <div class='header'>\n            <div class='name'>Sarah Johnson</div>\n            <div class='contact'>sarah.johnson@email.com • +1-555-0198 • 123 Tech Street, San Francisco, CA 94102</div>\n            <div><a href='https://linkedin.com/in/sarahjohnson'>LinkedIn</a> | <a href='https://github.com/sarahjohnson'>GitHub</a> | <a href='https://sarahjohnson.dev'>Portfolio</a></div>\n        </div>\n        \n        <div class='section'>\n            <div class='section-title'>Professional Summary</div>\n            <p>Experienced full-stack developer with 5+ years of expertise in building scalable web applications using modern technologies. Passionate about clean code, user experience, and continuous learning.</p>\n        </div>\n        \n        <div class='section'>\n            <div class='section-title'>Professional Experience</div>\n            <div class='experience-item'>\n                <div class='job-title'>Senior Software Engineer</div>\n                <div class='company'>TechCorp Solutions - San Francisco, CA</div>\n                <div class='date-location'>March 2021 - January 2024</div>\n                <div class='description'>Led development of microservices architecture serving 1M+ daily active users</div>\n                <ul>\n                    <li>Reduced API response time by 45% through database optimization and caching strategies</li>\n                    <li>Mentored 4 junior developers and established code review best practices</li>\n                    <li>Designed and implemented real-time notification system handling 500K+ messages daily</li>\n                    <li>Led migration from monolith to microservices, improving deployment frequency by 300%</li>\n                </ul>\n            </div>\n            <!-- ... more experience entries ... -->\n        </div>\n        \n        <!-- ... education, skills, certifications, projects sections ... -->\n    </div>\n</body>\n</html>",
  "format": "Html",
  "aiReview": {
    "overallScore": 8,
    "strengths": [
      "Strong quantified achievements throughout experience section",
      "Clear progression from junior to senior roles",
      "Excellent technical skills coverage",
      "Professional formatting and structure",
      "Good balance of technical and leadership experience"
    ],
    "improvementSuggestions": [
      "Consider adding more specific metrics for project outcomes",
      "Include industry-specific keywords for ATS optimization",
      "Add a brief mention of soft skills in the summary",
      "Consider reorganizing skills by category (languages, frameworks, tools)"
    ],
    "sectionRecommendations": {
      "Professional Summary": [
        "Expand to include specific years of experience with key technologies",
        "Mention team leadership and mentoring experience"
      ],
      "Experience": [
        "Add more context about company size or industry impact",
        "Include revenue or cost savings achieved through optimizations"
      ],
      "Skills": [
        "Group skills by category for better readability",
        "Consider adding proficiency levels for key technologies"
      ],
      "Projects": [
        "Include user adoption metrics or technical challenges overcome",
        "Mention technologies used that align with target job requirements"
      ]
    },
    "keywordAnalysis": {
      "relevantKeywords": [
        "microservices", "scalable", "optimization", "mentoring", 
        "real-time", "migration", "full-stack", "TypeScript"
      ],
      "suggestedKeywords": [
        "agile", "devops", "performance tuning", "architecture design",
        "team leadership", "cross-functional collaboration"
      ],
      "keywordDensityScore": 7.8
    },
    "generalFeedback": "This is a strong resume that effectively showcases technical expertise and leadership growth. The quantified achievements are particularly impressive and demonstrate real business impact. To further strengthen the resume, consider adding more industry-specific keywords and expanding on the leadership aspects of your roles. The technical projects show initiative and personal development, which is excellent for senior engineering positions."
  },
  "metadata": {
    "templateId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
    "generatedAt": "2024-06-04T14:08:45Z",
    "processingTimeMs": 223500,
    "claudeApiVersion": "claude-sonnet-4-20250514",
    "openAiModel": "gpt-4o",
    "tokenUsage": {
      "claudeInputTokens": 2847,
      "claudeOutputTokens": 1523,
      "openAiInputTokens": 1689,
      "openAiOutputTokens": 847
    }
  }
}
```

### List All Jobs

Get a paginated list of all resume generation jobs.

**Endpoint:** `GET /api/resume/jobs`

#### Request Example

```http
GET /api/resume/jobs?status=Completed&pageNumber=1&pageSize=10
Accept: application/json
```

#### Response Example

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 156,
  "totalPages": 16,
  "hasPreviousPage": false,
  "hasNextPage": true,
  "items": [
    {
      "jobId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "templateName": "Modern Professional",
      "personName": "Sarah Johnson",
      "status": "Completed",
      "createdAt": "2024-06-04T14:05:00Z",
      "completedAt": "2024-06-04T14:08:45Z",
      "processingTimeMs": 223500
    },
    {
      "jobId": "e36dc42f-47bb-4261-9456-1d92a1b2c3d4",
      "templateName": "Creative Portfolio",
      "personName": "Alex Chen",
      "status": "Completed",
      "createdAt": "2024-06-04T13:45:00Z",
      "completedAt": "2024-06-04T13:48:12Z",
      "processingTimeMs": 192000
    }
    // ... more job summaries
  ]
}
```

---

## ❌ Error Responses

### Validation Errors

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "error": {
    "message": "Validation failed",
    "type": "ValidationException",
    "timestamp": "2024-06-04T14:15:00Z",
    "traceId": "0HN4FSRVB1Q8T:00000001",
    "details": {
      "PersonalInfo.Email": ["The Email field is not a valid e-mail address."],
      "Experience[0].StartDate": ["Start date cannot be in the future."]
    }
  }
}
```

### Resource Not Found

```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "error": {
    "message": "Template not found",
    "type": "ResourceNotFoundException",
    "timestamp": "2024-06-04T14:15:00Z",
    "traceId": "0HN4FSRVB1Q8T:00000002"
  }
}
```

### Rate Limit Exceeded

```http
HTTP/1.1 429 Too Many Requests
Content-Type: application/json
Retry-After: 60

{
  "error": {
    "message": "Rate limit exceeded. Please try again later.",
    "type": "RateLimitExceeded",
    "timestamp": "2024-06-04T14:15:00Z",
    "retryAfter": 60
  }
}
```

### Internal Server Error

```http
HTTP/1.1 500 Internal Server Error
Content-Type: application/json

{
  "error": {
    "message": "An internal server error occurred",
    "type": "InternalServerError",
    "timestamp": "2024-06-04T14:15:00Z",
    "traceId": "0HN4FSRVB1Q8T:00000003"
  }
}
```

---

## 📊 Pagination

All list endpoints support pagination with the following query parameters:

- `pageNumber`: Page number (default: 1)
- `pageSize`: Items per page (default: 10, max: 100)
- `status`: Filter by status (optional)

**Example:**
```http
GET /api/resume/jobs?pageNumber=2&pageSize=20&status=InProgress
```

**Response includes pagination metadata:**
```json
{
  "pageNumber": 2,
  "pageSize": 20,
  "totalCount": 156,
  "totalPages": 8,
  "hasPreviousPage": true,
  "hasNextPage": true,
  "items": [...]
}
```

---

## 🔗 Related Links

- [API Documentation (Swagger)](http://localhost:7001/swagger)
- [Health Check Endpoint](http://localhost:7001/health)
- [GitHub Repository](https://github.com/yourusername/resume-generator-api)

---

**Note**: Replace `localhost:7001` with your actual API base URL. All timestamps are in UTC format. API keys and sensitive data should be properly secured in production environments.