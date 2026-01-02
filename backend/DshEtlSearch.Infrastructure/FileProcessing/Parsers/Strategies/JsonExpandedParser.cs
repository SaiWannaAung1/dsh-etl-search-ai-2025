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
            var json = JsonSerializer.Deserialize<JsonElement>(content);

            // Navigate JSON structure safely
            var title = GetProperty(json, "title") ?? GetProperty(json, "name") ?? "Untitled";
            var description = GetProperty(json, "description") ?? GetProperty(json, "abstract") ?? "";
            var url = GetProperty(json, "url");

            // FIX: Return the DTO
            var dto = new ParsedMetadataDto
            {
                Title = title,
                Abstract = description,
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

    private string? GetProperty(JsonElement element, string propName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propName, out var prop))
        {
            return prop.ToString();
        }
        return null;
    }
}