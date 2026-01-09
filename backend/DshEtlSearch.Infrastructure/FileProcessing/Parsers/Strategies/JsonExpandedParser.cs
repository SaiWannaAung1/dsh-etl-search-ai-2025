using System.Text.Json;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies;

public class JsonExpandedParser : IMetadataParser
{
    public Result<ParsedMetadataDto> Parse(Stream content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // 1. Extract Title and Abstract (Requirement: 112)
            var title = GetProperty(root, "title") ?? GetProperty(root, "name") ?? "Untitled";
            var abstractText = GetProperty(root, "description") ?? GetProperty(root, "abstract");

            // 2. Extract Structured Authors (Requirement: Relationship Modeling)
            // Now returns List<AuthorAffiliation> instead of string?
            var authors = ExtractAuthorAffiliations(root);

            // 3. Extract Keywords (Requirement: 116 - Semantic Meaning)
            var keywords = ExtractKeywords(root);

            // 4. Discover URL Patterns (Requirement: 105)
            var url = DiscoverDownloadUrl(root) ?? GetProperty(root, "url");

            var dto = new ParsedMetadataDto
            {
                Title = title,
                Abstract = abstractText,
                Authors = JsonSerializer.Serialize(authors),
                Keywords = keywords,
                ResourceUrl = url,
                PublishedDate = DateTime.UtcNow
            };

            return Result<ParsedMetadataDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<ParsedMetadataDto>.Failure($"Failed to parse JSON: {ex.Message}");
        }
    }

    // --- Functional Helpers ---

    private string? GetProperty(JsonElement element, string propName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propName, out var prop))
        {
            // Use GetString() for strings to avoid extra double quotes
            return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
        }
        return null;
    }

    private List<AuthorAffiliation> ExtractAuthorAffiliations(JsonElement root)
    {
        var results = new List<AuthorAffiliation>();
        
        if (root.TryGetProperty("authors", out var array))
        {
            foreach (var item in array.EnumerateArray())
            {
                var name = GetProperty(item, "fullName");
                var org = GetProperty(item, "organisationName");

                if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(org))
                {
                    results.Add(new AuthorAffiliation
                    {
                        Name = name ?? "N/A",
                        Organization = org ?? "Independent"
                    });
                }
            }
        }
        
        // Remove duplicates if the same person/org pair appears twice
        return results.GroupBy(a => new { a.Name, a.Organization })
                      .Select(g => g.First())
                      .ToList();
    }

    private string? ExtractKeywords(JsonElement root)
    {
        var keys = new List<string>();
        string[] props = { "keywordsOther", "keywordsPlace" };
        
        foreach (var prop in props)
        {
            if (root.TryGetProperty(prop, out var array))
            {
                foreach (var item in array.EnumerateArray())
                {
                    var val = GetProperty(item, "value");
                    if (val != null) keys.Add(val);
                }
            }
        }
        return keys.Count > 0 ? string.Join(", ", keys.Distinct()) : null;
    }

    private string? DiscoverDownloadUrl(JsonElement root)
    {
        if (root.TryGetProperty("onlineResources", out var array))
        {
            foreach (var item in array.EnumerateArray())
            {
                if (GetProperty(item, "function") == "download")
                {
                    return GetProperty(item, "url");
                }
            }
        }
        return null;
    }
}