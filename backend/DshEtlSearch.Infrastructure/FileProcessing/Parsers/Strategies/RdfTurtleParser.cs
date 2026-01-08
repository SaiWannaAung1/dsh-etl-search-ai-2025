using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using System.Text.Json;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies;

public class RdfTurtleParser : IMetadataParser
{
    public Result<ParsedMetadataDto> Parse(Stream content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // 1. Find the main Dataset node
            var datasetNode = FindNodeByType(root, "Dataset");

            // 2. Extract Core Metadata
            var title = GetProperty(datasetNode, "name") ?? "Untitled Dataset";
            var description = GetProperty(datasetNode, "description");

            // 3. Extract Structured Authors (List<AuthorAffiliation>)
            var authorsList = ExtractAuthorsWithAffiliations(datasetNode, root);

            // 4. Keywords and URLs
            var keywordsList = ExtractKeywords(datasetNode);
            var downloadUrl = ExtractDownloadUrl(datasetNode, root);

            var dto = new ParsedMetadataDto
            {
                Title = title,
                Abstract = description,
                Authors = JsonSerializer.Serialize(authorsList), // Now a List<AuthorAffiliation>
                Keywords = keywordsList.Any() ? string.Join(", ", keywordsList) : null,
                ResourceUrl = downloadUrl,
                PublishedDate = ExtractDate(datasetNode)
            };

            return Result<ParsedMetadataDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<ParsedMetadataDto>.Failure($"Failed to parse RDF/JSON-LD: {ex.Message}");
        }
    }

    // --- Functional Helpers for Affiliation ---

    private List<AuthorAffiliation> ExtractAuthorsWithAffiliations(JsonElement datasetNode, JsonElement root)
    {
        var affiliations = new List<AuthorAffiliation>();
        
        if (datasetNode.TryGetProperty("creator", out var creators))
        {
            foreach (var creatorRef in creators.EnumerateArray())
            {
                var id = GetProperty(creatorRef, "@id");
                var personNode = FindNodeById(root, id);
                
                if (personNode.ValueKind != JsonValueKind.Undefined)
                {
                    var name = GetProperty(personNode, "name");
                    
                    // Look up the Organization via affiliation reference
                    string? orgName = null;
                    if (personNode.TryGetProperty("affiliation", out var aff))
                    {
                        var affId = GetProperty(aff, "@id");
                        var orgNode = FindNodeById(root, affId);
                        
                        // If orgNode wasn't found by ID, check if name is directly in affiliation object
                        orgName = GetProperty(orgNode, "name") ?? GetProperty(aff, "name");
                    }

                    if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(orgName))
                    {
                        affiliations.Add(new AuthorAffiliation
                        {
                            Name = name ?? "N/A",
                            Organization = orgName ?? "Independent / Private"
                        });
                    }
                }
            }
        }
        
        // Ensure distinct entries
        return affiliations.GroupBy(a => new { a.Name, a.Organization })
                           .Select(g => g.First())
                           .ToList();
    }

    // --- Base Helpers ---

    private string? GetProperty(JsonElement element, string propName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propName, out var prop))
        {
            return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
        }
        return null;
    }

    private JsonElement FindNodeByType(JsonElement root, string typeName)
    {
        if (root.TryGetProperty("@graph", out var graph))
        {
            foreach (var node in graph.EnumerateArray())
            {
                if (GetProperty(node, "@type") == typeName) return node;
            }
        }
        return root;
    }

    private JsonElement FindNodeById(JsonElement root, string? id)
    {
        if (id != null && root.TryGetProperty("@graph", out var graph))
        {
            foreach (var node in graph.EnumerateArray())
            {
                if (GetProperty(node, "@id") == id) return node;
            }
        }
        return default;
    }

    private List<string> ExtractKeywords(JsonElement datasetNode)
    {
        var keywords = new List<string>();
        if (datasetNode.TryGetProperty("keywords", out var kArray))
        {
            foreach (var k in kArray.EnumerateArray())
            {
                var val = k.ValueKind == JsonValueKind.String ? k.GetString() : GetProperty(k, "name");
                if (val != null) keywords.Add(val);
            }
        }
        return keywords.Distinct().ToList();
    }

    private string? ExtractDownloadUrl(JsonElement datasetNode, JsonElement root)
    {
        if (datasetNode.TryGetProperty("distribution", out var distArray))
        {
            var distId = GetProperty(distArray.EnumerateArray().FirstOrDefault(), "@id");
            var distNode = FindNodeById(root, distId);
            return GetProperty(distNode, "contentUrl");
        }
        return null;
    }

    private DateTime? ExtractDate(JsonElement datasetNode)
    {
        var dateStr = GetProperty(datasetNode, "datePublished");
        return DateTime.TryParse(dateStr, out var date) ? date : DateTime.UtcNow;
    }
}