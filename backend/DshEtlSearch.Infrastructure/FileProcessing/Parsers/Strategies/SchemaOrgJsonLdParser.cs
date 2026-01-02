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
            var json = JsonSerializer.Deserialize<JsonElement>(content);

            var title = json.TryGetProperty("name", out var n) ? n.GetString() : "Untitled Schema.org Dataset";
            var abstractText = json.TryGetProperty("description", out var d) ? d.GetString() : "No description.";
            var url = json.TryGetProperty("url", out var u) ? u.GetString() : "";

            // FIX: Return the DTO
            var dto = new ParsedMetadataDto
            {
                Title = title ?? "Untitled",
                Abstract = abstractText,
                ResourceUrl = url,
                PublishedDate = DateTime.UtcNow
            };

            return Result<ParsedMetadataDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<ParsedMetadataDto>.Failure($"Failed to parse JSON-LD: {ex.Message}");
        }
    }
}