using Microsoft.EntityFrameworkCore;
using ResumeGenerator.API.Data;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Entities;
using ResumeGenerator.API.Models.Enums;
using ResumeGenerator.API.Services.Interfaces;

namespace ResumeGenerator.API.Services.Implementation;

public class ResumeTemplateService : IResumeTemplateService
{
    private readonly ResumeGeneratorContext _context;
    private readonly IGoogleDocsService _googleDocsService;
    private readonly ILogger<ResumeTemplateService> _logger;

    public ResumeTemplateService(
        ResumeGeneratorContext context,
        IGoogleDocsService googleDocsService,
        ILogger<ResumeTemplateService> logger)
    {
        _context = context;
        _googleDocsService = googleDocsService;
        _logger = logger;
    }

    public async Task<IEnumerable<ResumeTemplateDto>> GetAllTemplatesAsync()
    {
        var templates = await _context.ResumeTemplates
            .Where(t => t.IsActive)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return templates.Select(MapToDto);
    }

    public async Task<ResumeTemplateDto?> GetTemplateByIdAsync(Guid id)
    {
        var template = await _context.ResumeTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        return template != null ? MapToDto(template) : null;
    }

    public async Task<ResumeTemplateDto> CreateTemplateAsync(CreateTemplateRequestDto request)
    {
        var template = new ResumeTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Content = request.Content,
            Format = request.Format,
            Tags = request.Tags,
            IsPublic = request.IsPublic
        };

        _context.ResumeTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created template {TemplateId} with name '{Name}'", template.Id, template.Name);
        return MapToDto(template);
    }

    public async Task<ResumeTemplateDto?> UpdateTemplateAsync(Guid id, UpdateTemplateRequestDto request)
    {
        var template = await _context.ResumeTemplates.FindAsync(id);
        if (template == null || !template.IsActive)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.Name))
            template.Name = request.Name;
        
        if (request.Description != null)
            template.Description = request.Description;
        
        if (!string.IsNullOrEmpty(request.Content))
            template.Content = request.Content;
        
        if (request.Format.HasValue)
            template.Format = request.Format.Value;
        
        if (request.Tags != null)
            template.Tags = request.Tags;
        
        if (request.IsPublic.HasValue)
            template.IsPublic = request.IsPublic.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated template {TemplateId}", id);
        return MapToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        var template = await _context.ResumeTemplates.FindAsync(id);
        if (template == null || !template.IsActive)
        {
            return false;
        }

        // Soft delete
        template.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted template {TemplateId}", id);
        return true;
    }

    public async Task<ResumeTemplateDto> ImportFromGoogleDocsAsync(GoogleDocsImportRequestDto request)
    {
        if (!_googleDocsService.IsValidGoogleDocsUrl(request.DocumentUrl))
        {
            throw new ArgumentException("Invalid Google Docs URL");
        }

        var content = await _googleDocsService.ImportTemplateFromGoogleDocsAsync(
            request.DocumentUrl, 
            request.ServiceAccountCredentials);

        var createRequest = new CreateTemplateRequestDto
        {
            Name = request.TemplateName,
            Description = request.Description,
            Content = content,
            Format = TemplateFormat.GoogleDocs,
            Tags = request.Tags,
            IsPublic = request.IsPublic
        };

        return await CreateTemplateAsync(createRequest);
    }

    public async Task IncrementUsageCountAsync(Guid templateId)
    {
        var template = await _context.ResumeTemplates.FindAsync(templateId);
        if (template != null)
        {
            template.UsageCount++;
            await _context.SaveChangesAsync();
        }
    }

    private static ResumeTemplateDto MapToDto(ResumeTemplate template)
    {
        return new ResumeTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Content = template.Content,
            Format = template.Format,
            Tags = template.Tags,
            IsPublic = template.IsPublic,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            UsageCount = template.UsageCount
        };
    }
}
