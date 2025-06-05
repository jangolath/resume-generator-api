using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ResumeGenerator.API.Models.Validation;

/// <summary>
/// Validates phone number format
/// </summary>
public class PhoneAttribute : ValidationAttribute
{
    private static readonly Regex PhoneRegex = new(
        @"^[\+]?[1-9][\d]{0,15}$|^[\+]?[(]?[\d\s\-\(\)]{10,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return true; // Allow null/empty for optional fields

        var phoneNumber = value.ToString()!.Trim();
        
        // Remove common formatting characters
        var cleanPhone = Regex.Replace(phoneNumber, @"[\s\-\(\)\.\+]", "");
        
        // Check if it's a valid length and contains only digits (possibly with leading +)
        return cleanPhone.Length >= 10 && cleanPhone.Length <= 15 && 
               PhoneRegex.IsMatch(phoneNumber);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field is not a valid phone number.";
    }
}

/// <summary>
/// Validates that a date is not in the future
/// </summary>
public class NotFutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
            return true; // Allow null for optional fields

        if (value is DateTime dateTime)
        {
            return dateTime.Date <= DateTime.Today;
        }

        if (value is DateOnly dateOnly)
        {
            return dateOnly <= DateOnly.FromDateTime(DateTime.Today);
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field cannot be a future date.";
    }
}

/// <summary>
/// Validates that a start date is before an end date
/// </summary>
public class DateRangeAttribute : ValidationAttribute
{
    public string StartDateProperty { get; set; }
    public string EndDateProperty { get; set; }
    public bool AllowSameDate { get; set; } = true;

    public DateRangeAttribute(string startDateProperty, string endDateProperty)
    {
        StartDateProperty = startDateProperty;
        EndDateProperty = endDateProperty;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var startDateProperty = validationContext.ObjectType.GetProperty(StartDateProperty);
        var endDateProperty = validationContext.ObjectType.GetProperty(EndDateProperty);

        if (startDateProperty == null || endDateProperty == null)
        {
            return new ValidationResult($"Unknown property: {StartDateProperty} or {EndDateProperty}");
        }

        var startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance);
        var endDateValue = endDateProperty.GetValue(validationContext.ObjectInstance);

        if (startDateValue == null || endDateValue == null)
        {
            return ValidationResult.Success; // Allow null values
        }

        DateTime startDate, endDate;

        // Handle different date types
        if (startDateValue is DateTime startDateTime && endDateValue is DateTime endDateTime)
        {
            startDate = startDateTime;
            endDate = endDateTime;
        }
        else if (startDateValue is DateOnly startDateOnly && endDateValue is DateOnly endDateOnly)
        {
            startDate = startDateOnly.ToDateTime(TimeOnly.MinValue);
            endDate = endDateOnly.ToDateTime(TimeOnly.MinValue);
        }
        else
        {
            return new ValidationResult("Invalid date format");
        }

        var isValid = AllowSameDate ? startDate <= endDate : startDate < endDate;

        if (!isValid)
        {
            var message = AllowSameDate 
                ? $"{StartDateProperty} must be on or before {EndDateProperty}"
                : $"{StartDateProperty} must be before {EndDateProperty}";
            
            return new ValidationResult(message);
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates GPA is within acceptable range
/// </summary>
public class GpaRangeAttribute : ValidationAttribute
{
    public double MinValue { get; set; } = 0.0;
    public double MaxValue { get; set; } = 4.0;

    public override bool IsValid(object? value)
    {
        if (value == null)
            return true; // Allow null for optional fields

        if (value is double gpa)
        {
            return gpa >= MinValue && gpa <= MaxValue;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must be between {MinValue:F1} and {MaxValue:F1}.";
    }
}

/// <summary>
/// Validates that a list has a minimum number of items
/// </summary>
public class MinListCountAttribute : ValidationAttribute
{
    public int MinCount { get; set; }

    public MinListCountAttribute(int minCount)
    {
        MinCount = minCount;
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
            return MinCount == 0;

        if (value is System.Collections.ICollection collection)
        {
            return collection.Count >= MinCount;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must contain at least {MinCount} item(s).";
    }
}

/// <summary>
/// Validates that a list has a maximum number of items
/// </summary>
public class MaxListCountAttribute : ValidationAttribute
{
    public int MaxCount { get; set; }

    public MaxListCountAttribute(int maxCount)
    {
        MaxCount = maxCount;
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
            return true;

        if (value is System.Collections.ICollection collection)
        {
            return collection.Count <= MaxCount;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must contain at most {MaxCount} item(s).";
    }
}

/// <summary>
/// Validates that a string contains only allowed characters
/// </summary>
public class AllowedCharactersAttribute : ValidationAttribute
{
    private readonly Regex _regex;

    public AllowedCharactersAttribute(string pattern)
    {
        _regex = new Regex(pattern, RegexOptions.Compiled);
    }

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return true; // Allow null/empty for optional fields

        return _regex.IsMatch(value.ToString()!);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field contains invalid characters.";
    }
}

/// <summary>
/// Validates that a string does not contain profanity or inappropriate content
/// </summary>
public class NoProfanityAttribute : ValidationAttribute
{
    private static readonly HashSet<string> ProfanityWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add profanity words here - this is a basic example
        // In production, you might use a more comprehensive list or external service
        "badword1", "badword2", "inappropriate"
    };

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return true;

        var text = value.ToString()!.ToLowerInvariant();
        
        return !ProfanityWords.Any(word => text.Contains(word));
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field contains inappropriate content.";
    }
}

/// <summary>
/// Validates that a URL is accessible (optional validation for external URLs)
/// </summary>
public class ValidUrlAttribute : ValidationAttribute
{
    public bool CheckAccessibility { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 5;

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return true; // Allow null/empty for optional fields

        var urlString = value.ToString()!;

        // Basic URL format validation
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
            return false;

        // Check if it's HTTP or HTTPS
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        // Optional accessibility check (be careful with this in production)
        if (CheckAccessibility)
        {
            return IsUrlAccessibleAsync(urlString).GetAwaiter().GetResult();
        }

        return true;
    }

    private async Task<bool> IsUrlAccessibleAsync(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
            
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false; // Assume inaccessible if any exception occurs
        }
    }

    public override string FormatErrorMessage(string name)
    {
        return CheckAccessibility 
            ? $"The {name} field must be a valid and accessible URL."
            : $"The {name} field must be a valid URL.";
    }
}

/// <summary>
/// Validates that skills list contains reasonable items
/// </summary>
public class ValidSkillsAttribute : ValidationAttribute
{
    public int MinLength { get; set; } = 2;
    public int MaxLength { get; set; } = 50;

    public override bool IsValid(object? value)
    {
        if (value == null)
            return true;

        if (value is not IEnumerable<string> skills)
            return false;

        foreach (var skill in skills)
        {
            if (string.IsNullOrWhiteSpace(skill))
                return false;

            if (skill.Length < MinLength || skill.Length > MaxLength)
                return false;

            // Check for reasonable skill format (letters, numbers, spaces, common symbols)
            if (!Regex.IsMatch(skill, @"^[a-zA-Z0-9\s\.\-\+\#]+$"))
                return false;
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field contains invalid skills. Each skill must be {MinLength}-{MaxLength} characters and contain only letters, numbers, and common symbols.";
    }
}

/// <summary>
/// Validates that years of experience is reasonable
/// </summary>
public class ReasonableExperienceAttribute : ValidationAttribute
{
    public int MaxYears { get; set; } = 60; // Maximum reasonable years of experience

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        // For experience entries, calculate years from dates
        var objectType = validationContext.ObjectType;
        var startDateProperty = objectType.GetProperty("StartDate");
        var endDateProperty = objectType.GetProperty("EndDate");

        if (startDateProperty?.GetValue(validationContext.ObjectInstance) is DateTime startDate)
        {
            var endDate = endDateProperty?.GetValue(validationContext.ObjectInstance) as DateTime? ?? DateTime.Now;
            var experience = (endDate - startDate).TotalDays / 365.25;

            if (experience > MaxYears)
            {
                return new ValidationResult($"Experience duration cannot exceed {MaxYears} years.");
            }

            if (experience < 0)
            {
                return new ValidationResult("End date must be after start date.");
            }
        }

        return ValidationResult.Success;
    }
}