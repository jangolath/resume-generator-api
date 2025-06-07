using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using ResumeGenerator.API.Configuration;
using ResumeGenerator.API.Models.DTOs;
using ResumeGenerator.API.Models.Enums;
using ResumeGenerator.API.Services.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace ResumeGenerator.API.Services.Implementation;

public class GoogleDocsService : IGoogleDocsService
{
    private readonly GoogleDocsSettings _settings;
    private readonly ILogger<GoogleDocsService> _logger;
    private readonly DocsService _docsService;
    private readonly DriveService _driveService;

    public GoogleDocsService(
        IOptions<GoogleDocsSettings> settings,
        ILogger<GoogleDocsService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Add detailed logging for debugging
        _logger.LogInformation("Initializing GoogleDocsService with configuration:");
        _logger.LogInformation("- UseCredentialsFile: {UseCredentialsFile}", _settings.UseCredentialsFile);
        _logger.LogInformation("- ServiceAccountCredentials: {CredentialsInfo}", 
            string.IsNullOrEmpty(_settings.ServiceAccountCredentials) ? "NOT SET" : 
            _settings.UseCredentialsFile ? $"FILE PATH: {_settings.ServiceAccountCredentials}" : "JSON CONTENT SET");
        _logger.LogInformation("- TemplateFolderId: {FolderId}", _settings.TemplateFolderId);
        _logger.LogInformation("- ApplicationName: {AppName}", _settings.ApplicationName);

        var credential = GetGoogleCredential();
        
        _docsService = new DocsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = _settings.ApplicationName,
        });

        _driveService = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = _settings.ApplicationName,
        });

        _logger.LogInformation("GoogleDocsService initialized successfully");
    }

    public async Task<IEnumerable<GoogleDocsTemplateDto>> GetTemplatesFromFolderAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.TemplateFolderId))
            {
                _logger.LogWarning("TemplateFolderId is not configured");
                return new List<GoogleDocsTemplateDto>();
            }

            _logger.LogInformation("Fetching templates from Google Drive folder: {FolderId}", _settings.TemplateFolderId);

            var request = _driveService.Files.List();
            request.Q = $"'{_settings.TemplateFolderId}' in parents and mimeType='application/vnd.google-apps.document' and trashed=false";
            request.Fields = "files(id,name,description,createdTime,modifiedTime,webViewLink)";
            request.OrderBy = "modifiedTime desc";

            var result = await request.ExecuteAsync();
            
            var templates = result.Files.Select(file => new GoogleDocsTemplateDto
            {
                Id = file.Id,
                Name = file.Name,
                Description = file.Description ?? "No description available",
                CreatedTime = file.CreatedTimeDateTimeOffset?.DateTime ?? DateTime.MinValue,
                ModifiedTime = file.ModifiedTimeDateTimeOffset?.DateTime ?? DateTime.MinValue,
                WebViewLink = file.WebViewLink,
                DocumentUrl = $"https://docs.google.com/document/d/{file.Id}"
            }).ToList();

            _logger.LogInformation("Found {Count} templates in Google Drive folder", templates.Count);
            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching templates from Google Drive folder");
            throw;
        }
    }

    public async Task<GoogleDocsTemplateDetailDto> GetTemplateDetailAsync(string documentId)
    {
        try
        {
            _logger.LogInformation("Getting template detail for document: {DocumentId}", documentId);

            // Get document content
            var docRequest = _docsService.Documents.Get(documentId);
            var document = await docRequest.ExecuteAsync();

            // Get file metadata
            var fileRequest = _driveService.Files.Get(documentId);
            fileRequest.Fields = "id,name,description,createdTime,modifiedTime,webViewLink,size";
            var file = await fileRequest.ExecuteAsync();

            var htmlContent = ConvertDocumentToHtml(document);
            var plainTextContent = ConvertDocumentToPlainText(document);

            return new GoogleDocsTemplateDetailDto
            {
                Id = file.Id,
                Name = file.Name,
                Description = file.Description ?? "No description available",
                CreatedTime = file.CreatedTimeDateTimeOffset?.DateTime ?? DateTime.MinValue,
                ModifiedTime = file.ModifiedTimeDateTimeOffset?.DateTime ?? DateTime.MinValue,
                WebViewLink = file.WebViewLink,
                DocumentUrl = $"https://docs.google.com/document/d/{file.Id}",
                HtmlContent = htmlContent,
                PlainTextContent = plainTextContent,
                FileSize = file.Size
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template detail for document: {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<string> ImportTemplateFromGoogleDocsAsync(string documentUrl, string? credentials = null)
    {
        try
        {
            var documentId = ExtractDocumentIdFromUrl(documentUrl);
            if (string.IsNullOrEmpty(documentId))
            {
                throw new ArgumentException("Invalid Google Docs URL");
            }

            _logger.LogInformation("Importing template from Google Docs: {DocumentId}", documentId);

            var request = _docsService.Documents.Get(documentId);
            var document = await request.ExecuteAsync();

            var htmlContent = ConvertDocumentToHtml(document);
            
            _logger.LogInformation("Successfully imported template from Google Docs: {DocumentId}", documentId);
            return htmlContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing template from Google Docs: {Url}", documentUrl);
            throw;
        }
    }

    public async Task<string> ConvertToGoogleDocsFormatAsync(string content, OutputFormat format)
    {
        _logger.LogInformation("Converting content to Google Docs format from {Format}", format);
        
        return format switch
        {
            OutputFormat.Html => await ConvertHtmlToGoogleDocsAsync(content),
            OutputFormat.Markdown => await ConvertMarkdownToGoogleDocsAsync(content),
            OutputFormat.PlainText => await ConvertPlainTextToGoogleDocsAsync(content),
            _ => content
        };
    }

    public bool IsValidGoogleDocsUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        return url.Contains("docs.google.com/document") || 
               url.Contains("drive.google.com/file") ||
               Regex.IsMatch(url, @"^[a-zA-Z0-9-_]+$"); // Just document ID
    }

    private GoogleCredential GetGoogleCredential()
    {
        try
        {
            _logger.LogInformation("Creating Google credential...");

            GoogleCredential credential;

            if (_settings.UseCredentialsFile)
            {
                // Load from file
                _logger.LogInformation("Loading credentials from file: {FilePath}", _settings.ServiceAccountCredentials);
                
                if (!File.Exists(_settings.ServiceAccountCredentials))
                {
                    throw new FileNotFoundException($"Service account credentials file not found: {_settings.ServiceAccountCredentials}");
                }

                var fileInfo = new FileInfo(_settings.ServiceAccountCredentials);
                _logger.LogInformation("File exists, size: {Size} bytes", fileInfo.Length);

                try
                {
                    using var stream = new FileStream(_settings.ServiceAccountCredentials, FileMode.Open, FileAccess.Read);
                    credential = GoogleCredential.FromStream(stream);
                    _logger.LogInformation("Successfully loaded credentials from file");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load credentials from file. File content preview (first 100 chars):");
                    try
                    {
                        var preview = File.ReadAllText(_settings.ServiceAccountCredentials).Substring(0, Math.Min(100, File.ReadAllText(_settings.ServiceAccountCredentials).Length));
                        _logger.LogError("File content preview: {Preview}...", preview);
                    }
                    catch (Exception previewEx)
                    {
                        _logger.LogError(previewEx, "Could not read file for preview");
                    }
                    throw;
                }
            }
            else
            {
                // Load from JSON content
                _logger.LogInformation("Loading credentials from JSON content");
                
                if (string.IsNullOrEmpty(_settings.ServiceAccountCredentials))
                {
                    throw new InvalidOperationException("Service account credentials JSON content is empty");
                }

                _logger.LogInformation("JSON content length: {Length} characters", _settings.ServiceAccountCredentials.Length);
                _logger.LogInformation("JSON content preview (first 100 chars): {Preview}...", 
                    _settings.ServiceAccountCredentials.Substring(0, Math.Min(100, _settings.ServiceAccountCredentials.Length)));

                try
                {
                    credential = GoogleCredential.FromJson(_settings.ServiceAccountCredentials);
                    _logger.LogInformation("Successfully loaded credentials from JSON content");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse JSON credentials. Full JSON content: {Json}", _settings.ServiceAccountCredentials);
                    throw;
                }
            }

            var scopedCredential = credential.CreateScoped(new[] 
            { 
                DocsService.Scope.Documents, 
                DriveService.Scope.DriveReadonly 
            });

            _logger.LogInformation("Successfully created scoped Google credential");
            return scopedCredential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Google credential");
            throw new InvalidOperationException("Failed to create Google credential. Check your service account configuration.", ex);
        }
    }

    private string ExtractDocumentIdFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        // Handle different URL formats
        var patterns = new[]
        {
            @"https://docs\.google\.com/document/d/([a-zA-Z0-9-_]+)",
            @"https://drive\.google\.com/file/d/([a-zA-Z0-9-_]+)",
            @"^([a-zA-Z0-9-_]+)$" // Just the ID
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(url, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return string.Empty;
    }

    private string ConvertDocumentToHtml(Document document)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine($"<title>{document.Title}</title>");
        html.AppendLine("<style>");
        html.AppendLine(GetDefaultStyles());
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        if (document.Body?.Content != null)
        {
            foreach (var element in document.Body.Content)
            {
                ProcessStructuralElement(element, html);
            }
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private string ConvertDocumentToPlainText(Document document)
    {
        var text = new StringBuilder();

        if (document.Body?.Content != null)
        {
            foreach (var element in document.Body.Content)
            {
                ExtractPlainText(element, text);
            }
        }

        return text.ToString().Trim();
    }

    private void ProcessStructuralElement(StructuralElement element, StringBuilder html)
    {
        if (element.Paragraph != null)
        {
            ProcessParagraph(element.Paragraph, html);
        }
        else if (element.Table != null)
        {
            ProcessTable(element.Table, html);
        }
        else if (element.SectionBreak != null)
        {
            html.AppendLine("<hr>");
        }
    }

    private void ProcessParagraph(Paragraph paragraph, StringBuilder html)
    {
        if (paragraph.Elements == null || !paragraph.Elements.Any())
        {
            html.AppendLine("<p></p>");
            return;
        }

        var style = paragraph.ParagraphStyle;
        var tagName = GetHtmlTagForParagraphStyle(style);
        
        html.Append($"<{tagName}>");

        foreach (var element in paragraph.Elements)
        {
            if (element.TextRun != null)
            {
                ProcessTextRun(element.TextRun, html);
            }
        }

        html.AppendLine($"</{tagName}>");
    }

    private void ProcessTextRun(TextRun textRun, StringBuilder html)
    {
        var content = textRun.Content ?? string.Empty;
        var style = textRun.TextStyle;

        // Handle line breaks
        content = content.Replace("\n", "<br>");
        content = content.Replace("\r", "");

        // Apply text formatting
        if (style != null)
        {
            if (style.Bold == true)
                content = $"<strong>{content}</strong>";
            if (style.Italic == true)
                content = $"<em>{content}</em>";
            if (style.Underline == true)
                content = $"<u>{content}</u>";
        }

        html.Append(content);
    }

    private void ProcessTable(Table table, StringBuilder html)
    {
        html.AppendLine("<table>");
        
        if (table.TableRows != null)
        {
            foreach (var row in table.TableRows)
            {
                html.AppendLine("<tr>");
                
                if (row.TableCells != null)
                {
                    foreach (var cell in row.TableCells)
                    {
                        html.Append("<td>");
                        
                        if (cell.Content != null)
                        {
                            foreach (var element in cell.Content)
                            {
                                ProcessStructuralElement(element, html);
                            }
                        }
                        
                        html.AppendLine("</td>");
                    }
                }
                
                html.AppendLine("</tr>");
            }
        }
        
        html.AppendLine("</table>");
    }

    private string GetHtmlTagForParagraphStyle(ParagraphStyle? style)
    {
        if (style?.NamedStyleType != null)
        {
            return style.NamedStyleType switch
            {
                "HEADING_1" => "h1",
                "HEADING_2" => "h2",
                "HEADING_3" => "h3",
                "HEADING_4" => "h4",
                "HEADING_5" => "h5",
                "HEADING_6" => "h6",
                "TITLE" => "h1",
                "SUBTITLE" => "h2",
                _ => "p"
            };
        }

        return "p";
    }

    private void ExtractPlainText(StructuralElement element, StringBuilder text)
    {
        if (element.Paragraph != null)
        {
            if (element.Paragraph.Elements != null)
            {
                foreach (var paragraphElement in element.Paragraph.Elements)
                {
                    if (paragraphElement.TextRun != null)
                    {
                        text.Append(paragraphElement.TextRun.Content ?? string.Empty);
                    }
                }
            }
            text.AppendLine();
        }
        else if (element.Table != null && element.Table.TableRows != null)
        {
            foreach (var row in element.Table.TableRows)
            {
                if (row.TableCells != null)
                {
                    foreach (var cell in row.TableCells)
                    {
                        if (cell.Content != null)
                        {
                            foreach (var cellElement in cell.Content)
                            {
                                ExtractPlainText(cellElement, text);
                            }
                        }
                        text.Append("\t");
                    }
                }
                text.AppendLine();
            }
        }
    }

    private string GetDefaultStyles()
    {
        return @"
        body { 
            font-family: Arial, sans-serif; 
            line-height: 1.6; 
            margin: 40px; 
            color: #333;
        }
        h1, h2, h3, h4, h5, h6 { 
            color: #2c3e50; 
            margin-top: 1.5em; 
            margin-bottom: 0.5em; 
        }
        p { 
            margin-bottom: 1em; 
        }
        table { 
            border-collapse: collapse; 
            width: 100%; 
            margin: 1em 0; 
        }
        td, th { 
            border: 1px solid #ddd; 
            padding: 8px; 
            text-align: left; 
        }
        strong { 
            font-weight: bold; 
        }
        em { 
            font-style: italic; 
        }
        u { 
            text-decoration: underline; 
        }
        ";
    }

    private async Task<string> ConvertHtmlToGoogleDocsAsync(string htmlContent)
    {
        await Task.CompletedTask;
        return htmlContent;
    }

    private async Task<string> ConvertMarkdownToGoogleDocsAsync(string markdownContent)
    {
        await Task.CompletedTask;
        
        return markdownContent
            .Replace("# ", "<h1>").Replace("\n", "</h1>\n")
            .Replace("## ", "<h2>").Replace("\n", "</h2>\n")
            .Replace("### ", "<h3>").Replace("\n", "</h3>\n")
            .Replace("**", "<strong>").Replace("**", "</strong>")
            .Replace("*", "<em>").Replace("*", "</em>");
    }

    private async Task<string> ConvertPlainTextToGoogleDocsAsync(string plainTextContent)
    {
        await Task.CompletedTask;
        
        var lines = plainTextContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join("\n", lines.Select(line => $"<p>{line.Trim()}</p>"));
    }

    public void Dispose()
    {
        _docsService?.Dispose();
        _driveService?.Dispose();
    }
}