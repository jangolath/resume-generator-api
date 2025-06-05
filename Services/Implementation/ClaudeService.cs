using Microsoft.Extensions.Options;
using ResumeGenerator.API.Configuration;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace ResumeGenerator.API.Services.Implementation;

/// <summary>
/// Service for interacting with Claude API
/// </summary>
public class ClaudeService : IClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly ClaudeApiSettings _settings;
    private readonly ILogger<ClaudeService> _logger;

    public ClaudeService(
        HttpClient httpClient,
        IOptions<ClaudeApiSettings> settings,
        ILogger<ClaudeService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", _settings.ApiVersion);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<string> GenerateResumeContentAsync(
        string template, 
        ResumeGenerationRequestDto personalData, 
        string? customInstructions = null)
    {
        try
        {
            _logger.LogInformation("Starting Claude resume generation for template");

            var prompt = BuildResumeGenerationPrompt(template, personalData, customInstructions);
            return await CallClaudeApiAsync(prompt, "resume generation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Claude resume generation");
            throw;
        }
    }

    public async Task<string> GenerateCoverLetterAsync(
        ResumeGenerationRequestDto personalData,
        string? customInstructions = null)
    {
        try
        {
            _logger.LogInformation("Starting Claude cover letter generation");

            var prompt = BuildCoverLetterPrompt(personalData, customInstructions);
            return await CallClaudeApiAsync(prompt, "cover letter generation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Claude cover letter generation");
            throw;
        }
    }

    private async Task<string> CallClaudeApiAsync(string prompt, string operationType)
    {
        var requestBody = new
        {
            model = _settings.Model,
            max_tokens = _settings.MaxTokens,
            temperature = _settings.Temperature,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await RetryOperation(async () =>
        {
            var httpResponse = await _httpClient.PostAsync("/v1/messages", content);
            httpResponse.EnsureSuccessStatusCode();
            return httpResponse;
        });

        var responseContent = await response.Content.ReadAsStringAsync();
        var claudeResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        if (claudeResponse?.Content?.FirstOrDefault()?.Text == null)
        {
            throw new InvalidOperationException("Claude API returned invalid response format");
        }

        var generatedContent = claudeResponse.Content.First().Text;
        
        _logger.LogInformation("Claude {OperationType} completed successfully. Tokens used: {InputTokens}/{OutputTokens}",
            operationType, claudeResponse.Usage?.InputTokens, claudeResponse.Usage?.OutputTokens);

        return generatedContent ?? throw new InvalidOperationException("Claude API returned empty content");
    }

    public async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            var testRequest = new
            {
                model = _settings.Model,
                max_tokens = 1,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = "test"
                    }
                }
            };

            var json = JsonSerializer.Serialize(testRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/v1/messages", content);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude API availability check failed");
            return false;
        }
    }

    public async Task<ApiUsageStatsDto> GetUsageStatsAsync()
    {
        // Claude doesn't provide usage stats endpoint directly
        // This would typically be tracked internally or via a monitoring service
        return new ApiUsageStatsDto
        {
            RequestsToday = 0, // Would be tracked internally
            TokensUsedToday = 0, // Would be tracked internally
            AverageResponseTimeMs = 0, // Would be tracked internally
            ErrorsToday = 0, // Would be tracked internally
            IsHealthy = await IsApiAvailableAsync()
        };
    }

    private static string BuildResumeGenerationPrompt(
        string template, 
        ResumeGenerationRequestDto personalData, 
        string? customInstructions)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("You are an expert resume writer and career consultant. Your task is to generate a professional, compelling resume using the provided template and personal information.");
        
        // Add job-specific context if provided
        if (personalData.JobDescription != null)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("IMPORTANT: This resume is being tailored for a specific job opportunity. Please optimize the content to align with the job requirements while maintaining authenticity.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("TARGET JOB INFORMATION:");
            promptBuilder.AppendLine($"Job Title: {personalData.JobDescription.JobTitle}");
            promptBuilder.AppendLine($"Company: {personalData.JobDescription.Company}");
            if (!string.IsNullOrEmpty(personalData.JobDescription.Industry))
                promptBuilder.AppendLine($"Industry: {personalData.JobDescription.Industry}");
            
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("JOB DESCRIPTION:");
            promptBuilder.AppendLine(personalData.JobDescription.Description);
            
            if (personalData.JobDescription.RequiredSkills?.Any() == true)
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("REQUIRED SKILLS:");
                promptBuilder.AppendLine(string.Join(", ", personalData.JobDescription.RequiredSkills));
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
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("INSTRUCTIONS:");
        promptBuilder.AppendLine("1. Use the provided HTML template as the structure");
        promptBuilder.AppendLine("2. Replace all placeholder variables ({{...}}) with the corresponding data");
        promptBuilder.AppendLine("3. Ensure all content is professional, accurate, and compelling");
        promptBuilder.AppendLine("4. Optimize the content for ATS (Applicant Tracking Systems)");
        
        if (personalData.JobDescription != null)
        {
            promptBuilder.AppendLine("5. Emphasize experience and skills that align with the job requirements");
            promptBuilder.AppendLine("6. Use keywords from the job description naturally throughout the resume");
            promptBuilder.AppendLine("7. Highlight accomplishments that demonstrate relevant capabilities");
            promptBuilder.AppendLine("8. Tailor the professional summary to address the specific role");
        }
        else
        {
            promptBuilder.AppendLine("5. Use action verbs and quantify achievements where possible");
        }
        
        promptBuilder.AppendLine("6. Maintain consistent formatting and style");
        promptBuilder.AppendLine("7. Return only the final HTML content, no explanations or additional text");
        promptBuilder.AppendLine();

        if (!string.IsNullOrEmpty(customInstructions))
        {
            promptBuilder.AppendLine("ADDITIONAL INSTRUCTIONS:");
            promptBuilder.AppendLine(customInstructions);
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("TEMPLATE:");
        promptBuilder.AppendLine(template);
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("PERSONAL DATA:");
        promptBuilder.AppendLine(JsonSerializer.Serialize(personalData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("REQUIREMENTS FOR OUTPUT:");
        promptBuilder.AppendLine("- Return valid, complete HTML");
        promptBuilder.AppendLine("- Ensure all data is properly integrated");
        promptBuilder.AppendLine("- Use professional language and tone");
        promptBuilder.AppendLine("- Format dates consistently (MM/YYYY format)");
        promptBuilder.AppendLine("- Handle conditional sections (only show if data exists)");
        promptBuilder.AppendLine("- Ensure proper HTML escaping for special characters");
        
        if (personalData.JobDescription != null)
        {
            promptBuilder.AppendLine("- Naturally incorporate relevant keywords from the job description");
            promptBuilder.AppendLine("- Emphasize transferable skills and relevant experience");
        }
        
        return promptBuilder.ToString();
    }

    private string BuildCoverLetterPrompt(
        ResumeGenerationRequestDto personalData,
        string? customInstructions)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("You are an expert cover letter writer and career consultant. Your task is to generate a compelling, personalized cover letter that complements the resume and demonstrates genuine interest in the specific role and company.");
        
        if (personalData.JobDescription == null)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("IMPORTANT: No specific job description was provided, so create a versatile cover letter that can be adapted for various opportunities in the candidate's field.");
        }
        else
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("IMPORTANT: This cover letter should be specifically tailored to the job opportunity described below.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("TARGET JOB INFORMATION:");
            promptBuilder.AppendLine($"Job Title: {personalData.JobDescription.JobTitle}");
            promptBuilder.AppendLine($"Company: {personalData.JobDescription.Company}");
            if (!string.IsNullOrEmpty(personalData.JobDescription.Location))
                promptBuilder.AppendLine($"Location: {personalData.JobDescription.Location}");
            if (!string.IsNullOrEmpty(personalData.JobDescription.Industry))
                promptBuilder.AppendLine($"Industry: {personalData.JobDescription.Industry}");
            
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("JOB DESCRIPTION:");
            promptBuilder.AppendLine(personalData.JobDescription.Description);
            
            if (personalData.JobDescription.RequiredQualifications?.Any() == true)
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("KEY REQUIREMENTS:");
                foreach (var qual in personalData.JobDescription.RequiredQualifications)
                {
                    promptBuilder.AppendLine($"- {qual}");
                }
            }
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("COVER LETTER REQUIREMENTS:");
        promptBuilder.AppendLine("1. Professional business letter format with proper addressing");
        promptBuilder.AppendLine("2. Compelling opening that grabs attention and shows enthusiasm");
        promptBuilder.AppendLine("3. 2-3 body paragraphs that tell a story and demonstrate value");
        promptBuilder.AppendLine("4. Specific examples that align with job requirements");
        promptBuilder.AppendLine("5. Professional closing with a clear call to action");
        promptBuilder.AppendLine("6. Maintain consistent tone - professional yet personable");
        promptBuilder.AppendLine("7. Length: 3-4 paragraphs, approximately 250-400 words");
        
        if (personalData.JobDescription != null)
        {
            promptBuilder.AppendLine("8. Address specific company needs mentioned in the job posting");
            promptBuilder.AppendLine("9. Use terminology and keywords from the job description");
            promptBuilder.AppendLine("10. Show knowledge of the company and role");
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("STRUCTURE GUIDELINES:");
        promptBuilder.AppendLine("- Paragraph 1: Hook + Position interest + Brief value proposition");
        promptBuilder.AppendLine("- Paragraph 2: Relevant experience + Specific achievement + Connection to role");
        promptBuilder.AppendLine("- Paragraph 3: Additional value + Cultural fit + Company research insight");
        promptBuilder.AppendLine("- Paragraph 4: Enthusiasm + Next steps + Professional closing");
        
        if (!string.IsNullOrEmpty(customInstructions))
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("ADDITIONAL INSTRUCTIONS:");
            promptBuilder.AppendLine(customInstructions);
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("CANDIDATE INFORMATION:");
        promptBuilder.AppendLine(JsonSerializer.Serialize(personalData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("OUTPUT REQUIREMENTS:");
        promptBuilder.AppendLine("- Return properly formatted HTML for the cover letter");
        promptBuilder.AppendLine("- Include proper business letter structure");
        promptBuilder.AppendLine("- Use professional styling that complements the resume");
        promptBuilder.AppendLine("- Ensure all personal information is accurately integrated");
        promptBuilder.AppendLine("- Make it compelling and authentic, not generic");
        promptBuilder.AppendLine("- Focus on achievements and value, not just responsibilities");
        
        return promptBuilder.ToString();
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
                _logger.LogWarning(ex, "Claude API attempt {Attempt} failed", attempts);

                if (attempts >= _settings.RetryAttempts)
                    break;

                var delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMs * Math.Pow(2, attempts - 1));
                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException($"Claude API failed after {_settings.RetryAttempts} attempts", lastException);
    }

    // Claude API Response Models
    private class ClaudeApiResponse
    {
        public ClaudeContent[]? Content { get; set; }
        public ClaudeUsage? Usage { get; set; }
        public string? Model { get; set; }
        public string? Role { get; set; }
    }

    private class ClaudeContent
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
    }

    private class ClaudeUsage
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
}