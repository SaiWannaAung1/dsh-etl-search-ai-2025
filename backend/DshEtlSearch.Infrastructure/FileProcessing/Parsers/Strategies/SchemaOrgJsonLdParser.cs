using System.Text.Json;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies
{
    public class SchemaOrgJsonLdParser : IMetadataParser
    {
        public async Task<Result<MetadataRecord>> ParseAsync(Stream content)
        {
            try
            {
                var doc = await JsonSerializer.DeserializeAsync<JsonElement>(content);

                // Basic JSON-LD Validation
                if (!doc.TryGetProperty("@context", out _) || !doc.TryGetProperty("@type", out _))
                {
                    return Result<MetadataRecord>.Failure("Invalid JSON-LD: Missing @context or @type");
                }

                // FIX: Safe extraction for Title (Required)
                string title = doc.TryGetProperty("name", out var n) && n.GetString() is string s 
                    ? s 
                    : "No Name";

                // FIX: Nullable extraction for Optional fields
                string? description = doc.TryGetProperty("description", out var d) ? d.GetString() : null;
                string? keywords = doc.TryGetProperty("keywords", out var k) ? k.GetString() : null;

                var record = new MetadataRecord
                {
                    Title = title,
                    Abstract = description,
                    Keywords = keywords,
                    SourceFormat = MetadataFormat.SchemaOrgJsonLd
                };

                return Result<MetadataRecord>.Success(record);
            }
            catch (Exception ex)
            {
                return Result<MetadataRecord>.Failure($"JSON-LD Error: {ex.Message}");
            }
        }
    }
}