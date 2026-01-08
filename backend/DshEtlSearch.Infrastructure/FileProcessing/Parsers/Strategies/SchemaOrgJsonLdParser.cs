using System.Text.Json;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies;

public class SchemaOrgJsonLdParser : IMetadataParser
{
    public Result<ParsedMetadataDto> Parse(Stream content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // 1. Core Metadata
            var title = GetProperty(root, "name") ?? "Untitled Schema.org Dataset";
            var abstractText = GetProperty(root, "description");

            // 2. Extract Structured Authors (Requirement: Relationship Modeling)
            var affiliations = ExtractAuthorsWithAffiliations(root);

            // 3. Extract Keywords (Requirement: Semantic Context)
            var keywords = ExtractKeywords(root);

            // 4. Resource URL
            var url = GetProperty(root, "url") ?? GetProperty(root, "isAccessibleForFree");

            var dto = new ParsedMetadataDto
            {
                Title = title,
                Abstract = abstractText,
                Authors = JsonSerializer.Serialize(affiliations), // List<AuthorAffiliation>
                Keywords = keywords,
                ResourceUrl = url,
                PublishedDate = ExtractDate(root)
            };

            return Result<ParsedMetadataDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<ParsedMetadataDto>.Failure($"Failed to parse JSON-LD: {ex.Message}");
        }
    }

    private string? GetProperty(JsonElement element, string propName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propName, out var prop))
        {
            return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
        }
        return null;
    }

    private List<AuthorAffiliation> ExtractAuthorsWithAffiliations(JsonElement root)
    {
        var results = new List<AuthorAffiliation>();
        string[] creatorProps = { "creator", "author" };

        foreach (var prop in creatorProps)
        {
            if (root.TryGetProperty(prop, out var element))
            {
                if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        AddAffiliationFromElement(item, results);
                    }
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    AddAffiliationFromElement(element, results);
                }
            }
        }
        
        return results.GroupBy(a => new { a.Name, a.Organization })
                      .Select(g => g.First())
                      .ToList();
    }

    private void AddAffiliationFromElement(JsonElement element, List<AuthorAffiliation> list)
    {
        var name = GetProperty(element, "name");
        
        // Look for nested organization (common in Schema.org)
        string? orgName = null;
        if (element.TryGetProperty("affiliation", out var aff))
        {
            orgName = GetProperty(aff, "name");
        }

        if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(orgName))
        {
            list.Add(new AuthorAffiliation
            {
                Name = name ?? "N/A",
                Organization = orgName ?? "Independent"
            });
        }
    }

    private string? ExtractKeywords(JsonElement root)
    {
        if (root.TryGetProperty("keywords", out var k))
        {
            if (k.ValueKind == JsonValueKind.Array)
            {
                var keys = k.EnumerateArray()
                            .Select(i => i.ValueKind == JsonValueKind.String ? i.GetString() : GetProperty(i, "name"))
                            .Where(v => v != null);
                return string.Join(", ", keys);
            }
            return k.GetString();
        }
        return null;
    }

    private DateTime? ExtractDate(JsonElement root)
    {
        var dateStr = GetProperty(root, "datePublished") ?? GetProperty(root, "dateModified");
        return DateTime.TryParse(dateStr, out var date) ? date : DateTime.UtcNow;
    }
}