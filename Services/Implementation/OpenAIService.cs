using Microsoft.Extensions.Options;
using ResumeGenerator.API.Configuration;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ResumeGenerator.API.Services.Implementation;

/// <summary>
/// Service for interacting with OpenAI API for resume review
/// </summary>
public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAISettings _settings;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(
        HttpClient httpClient,
        IOptions<OpenAISettings> settings,
        ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Enhanced debug logging
        _logger.LogInformation("OpenAI Service Configuration:");
        _logger.LogInformation("- Base URL: {BaseUrl}", _settings.BaseUrl);
        _logger.LogInformation("- Review Model: {Model}", _settings.ReviewModel);
        _logger.LogInformation("- API Key: {ApiKey}", 
            string.IsNullOrEmpty(_settings.ApiKey) ? "NOT SET" : $"SET (length: {_settings.ApiKey.Length}, starts with: {_settings.ApiKey.Substring(0, Math.Min(10, _settings.ApiKey.Length))}...)");
        _logger.LogInformation("- Organization ID: {OrgId}", 
            string.IsNullOrEmpty(_settings.OrganizationId) ? "NOT SET" : _settings.OrganizationId);
        _logger.LogInformation("- Timeout: {Timeout} seconds", _settings.TimeoutSeconds);

        ConfigureHttpClient();

        // Log configured headers
        _logger.LogInformation("Configured HTTP Headers:");
        foreach (var header in _httpClient.DefaultRequestHeaders)
        {
            // Don't log the full auth header for security
            if (header.Key == "Authorization")
            {
                // Don't add "Bearer" prefix since the header value already contains it
                _logger.LogInformation("- {Key}: {Value}", header.Key, 
                    header.Value.FirstOrDefault()?.Substring(0, Math.Min(20, header.Value.FirstOrDefault()?.Length ?? 0)) + "...");
            }
            else
            {
                _logger.LogInformation("- {Key}: {Value}", header.Key, string.Join(", ", header.Value));
            }
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        
        // Clear existing headers first
        _httpClient.DefaultRequestHeaders.Clear();
        
        // Add authorization header using the proper method
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            // Use AuthenticationHeaderValue instead of Add() - this is the correct way
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            _logger.LogDebug("Added Authorization header with API key");
        }
        else
        {
            _logger.LogError("OpenAI API key is not set!");
        }
        
        // Only add organization header if it's not empty
        if (!string.IsNullOrEmpty(_settings.OrganizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", _settings.OrganizationId);
            _logger.LogDebug("Added OpenAI-Organization header: {OrgId}", _settings.OrganizationId);
        }
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            _logger.LogDebug("Checking OpenAI API availability...");
            
            // Create a new request message to see exactly what's being sent
            var request = new HttpRequestMessage(HttpMethod.Get, "/v1/models");
            
            // Log the request details
            _logger.LogDebug("Request URI: {Uri}", request.RequestUri);
            _logger.LogDebug("Request headers:");
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                if (header.Key == "Authorization")
                {
                    _logger.LogDebug("- {Key}: {Value}", header.Key, "Bearer [REDACTED]");
                }
                else
                {
                    _logger.LogDebug("- {Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }
            }
            
            var response = await _httpClient.SendAsync(request);
            
            _logger.LogDebug("OpenAI API response: {StatusCode} - {ReasonPhrase}", 
                response.StatusCode, response.ReasonPhrase);
            
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error response: {Content}", content);
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API availability check failed with exception");
            return false;
        }
    }

    public async Task<AiReviewDto> ReviewResumeAsync(string resumeContent, ResumeGenerationRequestDto personalData)
    {
        try
        {
            _logger.LogInformation("Starting OpenAI resume review");

            var cleanContent = ExtractTextFromHtml(resumeContent);
            var prompt = BuildReviewPrompt(cleanContent, personalData);

            var review = await CallOpenAIAsync<AiReviewDto>(prompt, "resume review");

            // Enhance with keyword analysis
            review.KeywordAnalysis = AnalyzeKeywords(cleanContent, personalData);

            _logger.LogInformation("OpenAI resume review completed successfully");

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OpenAI resume review");
            throw;
        }
    }

    public async Task<CoverLetterReviewDto> ReviewCoverLetterAsync(string coverLetterContent, ResumeGenerationRequestDto personalData)
    {
        try
        {
            _logger.LogInformation("Starting OpenAI cover letter review");

            var cleanContent = ExtractTextFromHtml(coverLetterContent);
            var prompt = BuildCoverLetterReviewPrompt(cleanContent, personalData);

            var review = await CallOpenAIAsync<CoverLetterReviewDto>(prompt, "cover letter review");

            _logger.LogInformation("OpenAI cover letter review completed successfully");

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OpenAI cover letter review");
            throw;
        }
    }

    public async Task<JobMatchAnalysisDto> AnalyzeJobMatchAsync(string resumeContent, ResumeGenerationRequestDto personalData)
    {
        try
        {
            _logger.LogInformation("Starting OpenAI job match analysis");

            if (personalData.JobDescription == null)
            {
                throw new ArgumentException("Job description is required for job match analysis");
            }

            var cleanContent = ExtractTextFromHtml(resumeContent);
            var prompt = BuildJobMatchAnalysisPrompt(cleanContent, personalData);

            var analysis = await CallOpenAIAsync<JobMatchAnalysisDto>(prompt, "job match analysis");

            _logger.LogInformation("OpenAI job match analysis completed successfully");

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OpenAI job match analysis");
            throw;
        }
    }

    private async Task<T> CallOpenAIAsync<T>(string prompt, string operationType) where T : class
    {
        var requestBody = new
        {
            model = _settings.ReviewModel,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = GetSystemPromptForType<T>()
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            max_tokens = _settings.MaxTokens,
            temperature = _settings.Temperature,
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await RetryOperation(async () =>
        {
            var httpResponse = await _httpClient.PostAsync("/chat/completions", content);
            httpResponse.EnsureSuccessStatusCode();
            return httpResponse;
        });

        var responseContent = await response.Content.ReadAsStringAsync();
        var openAiResponse = JsonSerializer.Deserialize<OpenAIApiResponse>(responseContent);

        if (openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
        {
            throw new InvalidOperationException("OpenAI API returned invalid response format");
        }

        var reviewJson = openAiResponse.Choices.First().Message!.Content!;
        var result = JsonSerializer.Deserialize<T>(reviewJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (result == null)
        {
            throw new InvalidOperationException($"Failed to parse OpenAI {operationType} response");
        }

        _logger.LogInformation("OpenAI {OperationType} completed successfully. Tokens used: {PromptTokens}/{CompletionTokens}",
            operationType, openAiResponse.Usage?.PromptTokens, openAiResponse.Usage?.CompletionTokens);

        return result;
    }

    private string GetSystemPromptForType<T>()
    {
        return typeof(T).Name switch
        {
            nameof(AiReviewDto) => "You are an expert resume reviewer and career coach with extensive experience in hiring and recruiting across various industries. Provide detailed, actionable feedback to help improve resumes.",
            nameof(CoverLetterReviewDto) => "You are an expert cover letter reviewer and career consultant. Provide detailed feedback on cover letter effectiveness, personalization, and professional impact.",
            nameof(JobMatchAnalysisDto) => "You are an expert recruitment consultant and career advisor. Analyze how well a candidate's resume aligns with a specific job description and provide detailed matching insights.",
            _ => "You are an expert career consultant. Provide professional analysis and actionable feedback."
        };
    }

    public async Task<ApiUsageStatsDto> GetUsageStatsAsync()
    {
        // OpenAI doesn't provide usage stats endpoint directly
        // This would typically be tracked internally
        return new ApiUsageStatsDto
        {
            RequestsToday = 0, // Would be tracked internally
            TokensUsedToday = 0, // Would be tracked internally
            AverageResponseTimeMs = 0, // Would be tracked internally
            ErrorsToday = 0, // Would be tracked internally
            IsHealthy = await IsApiAvailableAsync()
        };
    }

    private string ExtractTextFromHtml(string htmlContent)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Remove script and style elements
            doc.DocumentNode
                .Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

            return doc.DocumentNode.InnerText;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text from HTML, using original content");
            return htmlContent;
        }
    }

    private string BuildReviewPrompt(string resumeContent, ResumeGenerationRequestDto personalData)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("Please review the following resume and provide detailed feedback. Your response must be valid JSON matching this exact structure:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"overallScore\": 8,");
        promptBuilder.AppendLine("  \"strengths\": [\"Clear formatting\", \"Strong action verbs\"],");
        promptBuilder.AppendLine("  \"improvementSuggestions\": [\"Add more quantified achievements\", \"Include more keywords\"],");
        promptBuilder.AppendLine("  \"sectionRecommendations\": {");
        promptBuilder.AppendLine("    \"Professional Summary\": [\"Make it more specific to target role\"],");
        promptBuilder.AppendLine("    \"Experience\": [\"Add more metrics and results\"]");
        promptBuilder.AppendLine("  },");
        promptBuilder.AppendLine("  \"generalFeedback\": \"Overall this is a solid resume...\"");
        promptBuilder.AppendLine("}");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("EVALUATION CRITERIA:");
        promptBuilder.AppendLine("1. Content Quality (25%): Relevance, accuracy, completeness");
        promptBuilder.AppendLine("2. Professional Presentation (20%): Formatting, consistency, readability");
        promptBuilder.AppendLine("3. Achievement Focus (25%): Quantified results, impact statements");
        promptBuilder.AppendLine("4. Keyword Optimization (15%): Industry-relevant terms, ATS compatibility");
        promptBuilder.AppendLine("5. Structure & Flow (15%): Logical organization, easy to scan");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("TARGET ROLE CONTEXT:");
        if (personalData.Experience?.Any() == true)
        {
            var latestJob = personalData.Experience.OrderByDescending(e => e.StartDate).First();
            promptBuilder.AppendLine($"Latest Role: {latestJob.JobTitle} at {latestJob.Company}");
        }
        
        if (personalData.Skills?.Any() == true)
        {
            promptBuilder.AppendLine($"Key Skills: {string.Join(", ", personalData.Skills.Take(5))}");
        }
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("RESUME CONTENT:");
        promptBuilder.AppendLine(resumeContent);
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("Provide specific, actionable feedback that will help improve this resume's effectiveness. Focus on:");
        promptBuilder.AppendLine("- Content gaps or weaknesses");
        promptBuilder.AppendLine("- Missing quantified achievements");
        promptBuilder.AppendLine("- Opportunities to better highlight skills and experience");
        promptBuilder.AppendLine("- Industry-specific improvements");
        promptBuilder.AppendLine("- ATS optimization suggestions");

        return promptBuilder.ToString();
    }

    private string BuildCoverLetterReviewPrompt(string coverLetterContent, ResumeGenerationRequestDto personalData)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("Please review the following cover letter and provide detailed feedback. Your response must be valid JSON matching this exact structure:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"overallScore\": 8,");
        promptBuilder.AppendLine("  \"strengths\": [\"Strong opening\", \"Good personalization\"],");
        promptBuilder.AppendLine("  \"improvementSuggestions\": [\"Add more specific examples\", \"Strengthen call to action\"],");
        promptBuilder.AppendLine("  \"toneFeedback\": \"Professional and engaging tone\",");
        promptBuilder.AppendLine("  \"personalizationScore\": 9,");
        promptBuilder.AppendLine("  \"generalFeedback\": \"This cover letter effectively communicates value...\"");
        promptBuilder.AppendLine("}");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("EVALUATION CRITERIA:");
        promptBuilder.AppendLine("1. Opening Impact (20%): Hook, enthusiasm, immediate value proposition");
        promptBuilder.AppendLine("2. Content Quality (30%): Relevant examples, specific achievements, storytelling");
        promptBuilder.AppendLine("3. Job Alignment (25%): Connection to role requirements, company research");
        promptBuilder.AppendLine("4. Professional Tone (15%): Appropriate formality, confident but not arrogant");
        promptBuilder.AppendLine("5. Structure & Flow (10%): Logical progression, easy to read, proper length");
        promptBuilder.AppendLine();

        if (personalData.JobDescription != null)
        {
            promptBuilder.AppendLine("TARGET ROLE CONTEXT:");
            promptBuilder.AppendLine($"Position: {personalData.JobDescription.JobTitle} at {personalData.JobDescription.Company}");
            if (!string.IsNullOrEmpty(personalData.JobDescription.Industry))
                promptBuilder.AppendLine($"Industry: {personalData.JobDescription.Industry}");
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("COVER LETTER CONTENT:");
        promptBuilder.AppendLine(coverLetterContent);
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("Provide specific, actionable feedback focusing on:");
        promptBuilder.AppendLine("- Personalization and company-specific content");
        promptBuilder.AppendLine("- Strength of examples and achievements");
        promptBuilder.AppendLine("- Professional tone and communication style");
        promptBuilder.AppendLine("- Overall impact and memorability");

        return promptBuilder.ToString();
    }

    private string BuildJobMatchAnalysisPrompt(string resumeContent, ResumeGenerationRequestDto personalData)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("Analyze how well this resume matches the provided job description. Your response must be valid JSON matching this structure:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"overallMatchScore\": 85,");
        promptBuilder.AppendLine("  \"skillsMatch\": {");
        promptBuilder.AppendLine("    \"matchingRequiredSkills\": [\"Python\", \"SQL\"],");
        promptBuilder.AppendLine("    \"missingRequiredSkills\": [\"Docker\"],");
        promptBuilder.AppendLine("    \"matchingPreferredSkills\": [\"React\"],");
        promptBuilder.AppendLine("    \"requiredSkillsMatchPercentage\": 80,");
        promptBuilder.AppendLine("    \"preferredSkillsMatchPercentage\": 60");
        promptBuilder.AppendLine("  },");
        promptBuilder.AppendLine("  \"experienceMatch\": {");
        promptBuilder.AppendLine("    \"experienceLevelMatch\": true,");
        promptBuilder.AppendLine("    \"relevantExperienceYears\": 5.5,");
        promptBuilder.AppendLine("    \"industryMatch\": true,");
        promptBuilder.AppendLine("    \"roleProgressionFeedback\": \"Strong career progression\",");
        promptBuilder.AppendLine("    \"matchingResponsibilities\": [\"Team leadership\", \"System design\"]");
        promptBuilder.AppendLine("  },");
        promptBuilder.AppendLine("  \"matchingKeywords\": [\"leadership\", \"scalable\"],");
        promptBuilder.AppendLine("  \"missingKeywords\": [\"agile\", \"microservices\"],");
        promptBuilder.AppendLine("  \"improvementRecommendations\": [\"Add agile methodology experience\"],");
        promptBuilder.AppendLine("  \"qualificationScore\": 90,");
        promptBuilder.AppendLine("  \"cultureFitIndicators\": [\"Collaborative experience\", \"Innovation focus\"]");
        promptBuilder.AppendLine("}");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("JOB DESCRIPTION:");
        promptBuilder.AppendLine($"Position: {personalData.JobDescription!.JobTitle}");
        promptBuilder.AppendLine($"Company: {personalData.JobDescription.Company}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(personalData.JobDescription.Description);
        
        if (personalData.JobDescription.RequiredSkills?.Any() == true)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("REQUIRED SKILLS:");
            promptBuilder.AppendLine(string.Join(", ", personalData.JobDescription.RequiredSkills));
        }
        
        if (personalData.JobDescription.PreferredSkills?.Any() == true)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("PREFERRED SKILLS:");
            promptBuilder.AppendLine(string.Join(", ", personalData.JobDescription.PreferredSkills));
        }
        
        if (personalData.JobDescription.RequiredQualifications?.Any() == true)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("REQUIRED QUALIFICATIONS:");
            foreach (var qual in personalData.JobDescription.RequiredQualifications)
            {
                promptBuilder.AppendLine($"- {qual}");
            }
        }
        
        if (personalData.JobDescription.Responsibilities?.Any() == true)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("KEY RESPONSIBILITIES:");
            foreach (var resp in personalData.JobDescription.Responsibilities)
            {
                promptBuilder.AppendLine($"- {resp}");
            }
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("CANDIDATE RESUME:");
        promptBuilder.AppendLine(resumeContent);
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("ANALYSIS REQUIREMENTS:");
        promptBuilder.AppendLine("- Calculate match percentages based on keyword overlap and context");
        promptBuilder.AppendLine("- Identify specific skills and qualifications matches/gaps");
        promptBuilder.AppendLine("- Evaluate experience relevance and level appropriateness");
        promptBuilder.AppendLine("- Provide actionable recommendations for improvement");
        promptBuilder.AppendLine("- Consider cultural fit indicators and soft skills");

        return promptBuilder.ToString();
    }

    private KeywordAnalysisDto AnalyzeKeywords(string resumeContent, ResumeGenerationRequestDto personalData)
    {
        var words = ExtractWords(resumeContent);
        var skillWords = personalData.Skills ?? new List<string>();
        
        // Simple keyword analysis - in production, this could be more sophisticated
        var relevantKeywords = words
            .Where(w => skillWords.Any(s => s.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .ToList();

        var suggestedKeywords = skillWords
            .Where(s => !words.Any(w => w.Contains(s, StringComparison.OrdinalIgnoreCase)))
            .Take(5)
            .ToList();

        var keywordDensity = relevantKeywords.Count > 0 ? 
            (double)relevantKeywords.Count / words.Count * 100 : 0;

        return new KeywordAnalysisDto
        {
            RelevantKeywords = relevantKeywords,
            SuggestedKeywords = suggestedKeywords,
            KeywordDensityScore = Math.Round(keywordDensity, 2)
        };
    }

    private List<string> ExtractWords(string text)
    {
        var words = Regex.Matches(text, @"\b\w+\b")
            .Cast<Match>()
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => w.Length > 2) // Filter out very short words
            .ToList();

        return words;
    }

    private async Task<HttpResponseMessage> RetryOperation(Func<Task<HttpResponseMessage>> operation)
    {
        var attempts = 0;
        Exception? lastException = null;

        while (attempts < _settings.RetryAttempts)
        {
            try
            {
                attempts++;
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "OpenAI API attempt {Attempt} failed", attempts);

                if (attempts >= _settings.RetryAttempts)
                    break;

                var delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMs * Math.Pow(2, attempts - 1));
                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException($"OpenAI API failed after {_settings.RetryAttempts} attempts", lastException);
    }

    // OpenAI API Response Models
    private class OpenAIApiResponse
    {
        public OpenAIChoice[]? Choices { get; set; }
        public OpenAIUsage? Usage { get; set; }
        public string? Model { get; set; }
    }

    private class OpenAIChoice
    {
        public OpenAIMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    private class OpenAIMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class OpenAIUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}