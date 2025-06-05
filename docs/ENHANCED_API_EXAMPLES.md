# Enhanced API Examples - Job-Tailored Resume & Cover Letter Generation

This document provides examples of the enhanced Resume Generator API that supports job description targeting and cover letter generation.

## 🎯 Job-Tailored Resume Generation

Generate a resume specifically tailored to a job posting with optional cover letter.

### Request with Job Description

```http
POST /api/resume/generate
Content-Type: application/json

{
  "templateId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "personalInfo": {
    "firstName": "Michael",
    "lastName": "Chen",
    "email": "michael.chen@email.com",
    "phone": "+1-555-0142",
    "address": "456 Innovation Drive, Austin, TX 78701",
    "linkedInUrl": "https://linkedin.com/in/michaelchen",
    "gitHubUrl": "https://github.com/michaelchen",
    "professionalSummary": "Full-stack software engineer with 7+ years of experience building scalable web applications and leading development teams."
  },
  "jobDescription": {
    "jobTitle": "Senior Full Stack Engineer",
    "company": "TechFlow Solutions",
    "location": "Austin, TX (Hybrid)",
    "jobType": "Full-time",
    "salaryRange": "$140,000 - $180,000",
    "description": "We're looking for a Senior Full Stack Engineer to join our growing platform team. You'll be responsible for building and maintaining our core SaaS platform that serves over 50,000 customers worldwide. The ideal candidate has experience with modern web technologies, cloud infrastructure, and enjoys mentoring junior developers.",
    "requiredQualifications": [
      "Bachelor's degree in Computer Science or equivalent experience",
      "5+ years of professional software development experience",
      "Strong experience with React and Node.js",
      "Experience with cloud platforms (AWS, Azure, or GCP)",
      "Experience with SQL and NoSQL databases",
      "Strong understanding of RESTful APIs and microservices",
      "Experience with Git version control and CI/CD pipelines"
    ],
    "preferredQualifications": [
      "Experience with TypeScript",
      "Knowledge of containerization (Docker, Kubernetes)",
      "Experience with monitoring and observability tools",
      "Previous experience mentoring junior developers",
      "Familiarity with agile development methodologies"
    ],
    "responsibilities": [
      "Design and implement new features for our core platform",
      "Collaborate with product and design teams to deliver user-centric solutions",
      "Mentor junior developers and conduct code reviews",
      "Participate in architectural decisions and technical planning",
      "Ensure code quality, performance, and security best practices",
      "Troubleshoot and resolve production issues"
    ],
    "requiredSkills": [
      "JavaScript", "TypeScript", "React", "Node.js", "Express",
      "PostgreSQL", "MongoDB", "AWS", "Docker", "Git"
    ],
    "preferredSkills": [
      "Kubernetes", "Redis", "GraphQL", "Jest", "Cypress",
      "Terraform", "DataDog", "New Relic"
    ],
    "industry": "SaaS/Technology",
    "experienceLevel": "Senior",
    "remotePolicy": "Hybrid (3 days in office)",
    "jobPostingUrl": "https://techflow.com/careers/senior-fullstack-engineer"
  },
  "experience": [
    {
      "jobTitle": "Senior Software Engineer",
      "company": "CloudTech Inc",
      "location": "Austin, TX",
      "startDate": "2021-01-15T00:00:00Z",
      "endDate": "2024-05-30T00:00:00Z",
      "isCurrentPosition": false,
      "description": "Led development of microservices architecture for e-commerce platform",
      "accomplishments": [
        "Architected and implemented microservices migration reducing response times by 40%",
        "Mentored team of 4 junior developers, improving code quality scores by 60%",
        "Built real-time analytics dashboard processing 1M+ events daily",
        "Established CI/CD pipeline reducing deployment time from 2 hours to 15 minutes"
      ],
      "technologies": [
        "React", "TypeScript", "Node.js", "Express", "PostgreSQL",
        "AWS", "Docker", "Kubernetes", "Redis", "Jest"
      ]
    },
    {
      "jobTitle": "Full Stack Developer",
      "company": "StartupCo",
      "location": "Austin, TX",
      "startDate": "2018-03-01T00:00:00Z",
      "endDate": "2020-12-31T00:00:00Z",
      "isCurrentPosition": false,
      "description": "Full-stack development for B2B SaaS platform serving 10K+ users",
      "accomplishments": [
        "Developed core platform features from conception to production",
        "Implemented automated testing suite achieving 95% code coverage",
        "Optimized database queries reducing average response time by 50%",
        "Collaborated with design team to improve user experience and retention"
      ],
      "technologies": [
        "JavaScript", "React", "Node.js", "MongoDB", "Express",
        "AWS Lambda", "DynamoDB", "Git", "Mocha"
      ]
    }
  ],
  "education": [
    {
      "institution": "University of Texas at Austin",
      "degree": "Bachelor of Science",
      "fieldOfStudy": "Computer Science",
      "location": "Austin, TX",
      "endDate": "2018-05-15T00:00:00Z",
      "gpa": 3.7,
      "achievements": [
        "Summa Cum Laude",
        "ACM Programming Competition Finalist",
        "Teaching Assistant for Data Structures course"
      ]
    }
  ],
  "skills": [
    "JavaScript", "TypeScript", "React", "Node.js", "Express",
    "PostgreSQL", "MongoDB", "AWS", "Docker", "Kubernetes",
    "Git", "Jest", "Cypress", "Redis", "GraphQL", "Terraform"
  ],
  "certifications": [
    {
      "name": "AWS Certified Solutions Architect",
      "issuingOrganization": "Amazon Web Services",
      "issueDate": "2023-08-15T00:00:00Z",
      "expirationDate": "2026-08-15T00:00:00Z"
    }
  ],
  "projects": [
    {
      "name": "DevOps Automation Platform",
      "description": "Open-source platform for automating deployment workflows with integrated monitoring",
      "gitHubUrl": "https://github.com/michaelchen/devops-platform",
      "technologies": ["Node.js", "React", "Docker", "Kubernetes", "PostgreSQL"],
      "keyFeatures": [
        "Automated CI/CD pipeline generation",
        "Real-time deployment monitoring",
        "Integration with major cloud providers"
      ]
    }
  ],
  "outputFormat": "Html",
  "includeAiReview": true,
  "generateCoverLetter": true,
  "customInstructions": "Emphasize leadership experience and align closely with the job requirements. Highlight mentoring experience and cloud infrastructure expertise."
}
```

### Enhanced Response with Job Analysis

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "jobId": "e47ac10b-58cc-4372-a567-0e02b2c3d480",
  "status": "Pending",
  "estimatedCompletion": "2024-06-04T15:13:00Z",
  "message": "Resume and cover letter generation job queued for processing",
  "content": null
}
```

### Get Enhanced Results

```http
GET /api/resume/job/e47ac10b-58cc-4372-a567-0e02b2c3d480/result
```

### Enhanced Response with Job Match Analysis

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "generatedContent": "<!DOCTYPE html>\n<html>\n<!-- Tailored resume HTML with job-specific optimizations -->\n</html>",
  "coverLetterContent": "<!DOCTYPE html>\n<html>\n<head>\n    <title>Cover Letter - Michael Chen</title>\n    <style>\n        body { font-family: 'Segoe UI', sans-serif; max-width: 800px; margin: 0 auto; padding: 40px; }\n        .header { margin-bottom: 30px; }\n        .date { margin-bottom: 20px; }\n        .address { margin-bottom: 30px; }\n        .salutation { margin-bottom: 20px; }\n        .body-paragraph { margin-bottom: 20px; line-height: 1.6; }\n        .closing { margin-top: 30px; }\n    </style>\n</head>\n<body>\n    <div class='header'>\n        <strong>Michael Chen</strong><br>\n        456 Innovation Drive<br>\n        Austin, TX 78701<br>\n        (555) 142-0000<br>\n        michael.chen@email.com\n    </div>\n    \n    <div class='date'>June 4, 2024</div>\n    \n    <div class='address'>\n        Hiring Manager<br>\n        TechFlow Solutions<br>\n        Austin, TX\n    </div>\n    \n    <div class='salutation'>Dear Hiring Manager,</div>\n    \n    <div class='body-paragraph'>\n        I am excited to apply for the Senior Full Stack Engineer position at TechFlow Solutions. With over 7 years of experience building scalable web applications and a proven track record of mentoring development teams, I am confident I can make a significant contribution to your platform team that serves 50,000+ customers worldwide.\n    </div>\n    \n    <div class='body-paragraph'>\n        In my recent role as Senior Software Engineer at CloudTech Inc, I led the architectural design and implementation of a microservices migration that reduced response times by 40% and improved system scalability. My experience with React, Node.js, and AWS directly aligns with your technical requirements, and I have extensive hands-on experience with containerization using Docker and Kubernetes. Additionally, I mentored a team of 4 junior developers, improving our code quality scores by 60% while fostering a collaborative learning environment.\n    </div>\n    \n    <div class='body-paragraph'>\n        What particularly excites me about TechFlow Solutions is your commitment to innovation and your hybrid work culture. My experience building real-time analytics dashboards processing over 1M events daily demonstrates my ability to work with the scale and complexity your platform demands. I am also passionate about code quality and best practices, having established CI/CD pipelines that reduced deployment time from 2 hours to 15 minutes.\n    </div>\n    \n    <div class='body-paragraph'>\n        I would welcome the opportunity to discuss how my technical expertise and leadership experience can contribute to TechFlow Solutions' continued growth. Thank you for your consideration, and I look forward to hearing from you.\n    </div>\n    \n    <div class='closing'>\n        Sincerely,<br>\n        Michael Chen\n    </div>\n</body>\n</html>",
  "format": "Html",
  "aiReview": {
    "overallScore": 9,
    "strengths": [
      "Excellent alignment with job requirements",
      "Strong quantified achievements throughout",
      "Clear career progression and leadership experience",
      "Perfect technical skills match",
      "Professional formatting optimized for ATS"
    ],
    "improvementSuggestions": [
      "Consider adding specific examples of agile methodology usage",
      "Mention specific monitoring tools used in production environments",
      "Add brief mention of security best practices experience"
    ],
    "sectionRecommendations": {
      "Professional Summary": [
        "Already well-tailored to the position",
        "Effectively highlights relevant experience years"
      ],
      "Experience": [
        "Excellent use of job-relevant keywords",
        "Strong quantified achievements that demonstrate impact",
        "Good emphasis on mentoring and leadership"
      ],
      "Skills": [
        "Perfect match with required and preferred skills",
        "Well-organized and comprehensive"
      ]
    },
    "keywordAnalysis": {
      "relevantKeywords": [
        "microservices", "React", "Node.js", "TypeScript", "AWS",
        "Docker", "Kubernetes", "mentoring", "CI/CD", "scalable"
      ],
      "suggestedKeywords": [
        "agile", "scrum", "observability", "monitoring",
        "security", "performance optimization"
      ],
      "keywordDensityScore": 9.2
    },
    "generalFeedback": "This is an exceptionally strong resume that demonstrates excellent alignment with the target position. The candidate's experience directly maps to the job requirements, with strong evidence of both technical expertise and leadership capabilities. The quantified achievements are particularly compelling and show real business impact."
  },
  "coverLetterReview": {
    "overallScore": 8,
    "strengths": [
      "Strong opening that shows genuine interest and immediate value",
      "Excellent company research and role-specific customization",
      "Compelling examples that directly address job requirements",
      "Professional tone that balances confidence with enthusiasm",
      "Strong closing with clear call to action"
    ],
    "improvementSuggestions": [
      "Could mention specific TechFlow products or recent company news",
      "Consider adding a brief mention of soft skills like communication",
      "Could strengthen the connection to company culture and values"
    ],
    "toneFeedback": "Professional, confident, and appropriately enthusiastic. The tone effectively conveys expertise while showing genuine interest in the opportunity.",
    "personalizationScore": 9,
    "generalFeedback": "This is a highly effective cover letter that successfully bridges the candidate's experience with the company's needs. The specific examples and quantified achievements make a compelling case for the candidate's fit for the role."
  },
  "jobMatchAnalysis": {
    "overallMatchScore": 92,
    "skillsMatch": {
      "matchingRequiredSkills": [
        "JavaScript", "TypeScript", "React", "Node.js", "Express",
        "PostgreSQL", "MongoDB", "AWS", "Docker", "Git"
      ],
      "missingRequiredSkills": [],
      "matchingPreferredSkills": [
        "TypeScript", "Docker", "Kubernetes", "Jest", "Cypress"
      ],
      "requiredSkillsMatchPercentage": 100,
      "preferredSkillsMatchPercentage": 83
    },
    "experienceMatch": {
      "experienceLevelMatch": true,
      "relevantExperienceYears": 7.2,
      "industryMatch": true,
      "roleProgressionFeedback": "Excellent career progression from Full Stack Developer to Senior Software Engineer, demonstrating growth in responsibility and technical leadership",
      "matchingResponsibilities": [
        "Feature development and implementation",
        "Mentoring junior developers",
        "Architectural decision participation",
        "Code quality and performance optimization",
        "Production issue resolution"
      ]
    },
    "matchingKeywords": [
      "microservices", "scalable", "mentoring", "cloud", "CI/CD",
      "performance", "architecture", "collaboration", "SaaS"
    ],
    "missingKeywords": [
      "agile", "scrum", "product management", "user-centric",
      "observability", "monitoring"
    ],
    "improvementRecommendations": [
      "Add specific examples of agile methodology usage",
      "Mention experience with monitoring and observability tools",
      "Highlight any product management or user research collaboration",
      "Include examples of customer-focused development decisions"
    ],
    "qualificationScore": 95,
    "cultureFitIndicators": [
      "Strong mentoring and collaboration experience",
      "Evidence of innovation and technical leadership",
      "Experience in fast-paced startup environments",
      "Commitment to code quality and best practices",
      "Proven ability to work with cross-functional teams"
    ]
  },
  "metadata": {
    "templateId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
    "generatedAt": "2024-06-04T14:12:45Z",
    "processingTimeMs": 287500,
    "claudeApiVersion": "claude-sonnet-4-20250514",
    "openAiModel": "gpt-4o",
    "tokenUsage": {
      "claudeInputTokens": 4250,
      "claudeOutputTokens": 2100,
      "openAiInputTokens": 2800,
      "openAiOutputTokens": 1450
    }
  }
}
```

## 🎯 Use Cases

### 1. **Job-Specific Resume Optimization**
- **Input**: Generic resume data + specific job posting
- **Output**: Tailored resume with optimized keywords and relevant experience emphasis
- **Benefits**: Higher ATS pass-through rates, better keyword matching

### 2. **Complete Application Package**
- **Input**: Resume data + job description + cover letter request
- **Output**: Matching resume and cover letter pair
- **Benefits**: Consistent messaging, professional presentation

### 3. **Application Strategy Insights**
- **Input**: Resume + detailed job requirements
- **Output**: Match analysis with improvement recommendations
- **Benefits**: Data-driven application strategy, skill gap identification

### 4. **Bulk Application Preparation**
- **Input**: Base resume data + multiple job descriptions
- **Output**: Multiple tailored resumes for different positions
- **Benefits**: Efficient job search workflow, position-specific optimization

## 🔧 Integration Examples

### Frontend Integration

```javascript
// React component example
const ResumeGenerator = () => {
  const [jobDescription, setJobDescription] = useState('');
  const [generateCoverLetter, setGenerateCoverLetter] = useState(true);
  
  const generateResume = async (personalData) => {
    const request = {
      ...personalData,
      jobDescription: parseJobDescription(jobDescription),
      generateCoverLetter,
      includeAiReview: true,
      outputFormat: 'Html'
    };
    
    const response = await fetch('/api/resume/generate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    });
    
    const result = await response.json();
    return result;
  };
  
  return (
    <form onSubmit={handleSubmit}>
      <textarea 
        placeholder="Paste job description here..."
        value={jobDescription}
        onChange={(e) => setJobDescription(e.target.value)}
      />
      <label>
        <input 
          type="checkbox" 
          checked={generateCoverLetter}
          onChange={(e) => setGenerateCoverLetter(e.target.checked)}
        />
        Generate Cover Letter
      </label>
      <button type="submit">Generate Tailored Resume</button>
    </form>
  );
};
```

### Job Description Parser

```javascript
// Utility to parse job postings from various formats
const parseJobDescription = (text) => {
  // Extract structured data from job posting text
  const lines = text.split('\n');
  
  const jobData = {
    jobTitle: extractJobTitle(lines),
    company: extractCompany(lines),
    description: text,
    requiredSkills: extractSkills(text, 'required'),
    preferredSkills: extractSkills(text, 'preferred'),
    responsibilities: extractResponsibilities(text)
  };
  
  return jobData;
};
```

## 📊 Performance Considerations

### Processing Time Estimates
- **Resume Only**: 15-30 seconds
- **Resume + Cover Letter**: 25-45 seconds  
- **Full Package with Analysis**: 35-60 seconds

### Token Usage
- **Job-Tailored Resume**: ~2-3x standard resume tokens
- **Cover Letter**: ~1,500-2,000 tokens
- **Job Match Analysis**: ~1,000-1,500 tokens

### Cost Optimization
- Use job description caching for similar roles
- Implement smart batching for multiple applications
- Optional AI review for cost-sensitive scenarios

---

**This enhanced API transforms the resume generation experience from generic document creation to intelligent, job-specific application optimization.**