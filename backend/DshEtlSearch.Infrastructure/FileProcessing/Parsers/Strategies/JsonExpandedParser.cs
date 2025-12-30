using System.Text.Json;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies
{
    public class JsonExpandedParser : IMetadataParser
    {
        public async Task<Result<MetadataRecord>> ParseAsync(Stream content)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var doc = await JsonSerializer.DeserializeAsync<JsonElement>(content, options);

                // FIX: Use pattern matching to safely extract non-null string
                // Logic: If property "name" exists AND its value is a valid string 's', use 's'. Else "No Title".
                string title = doc.TryGetProperty("name", out var nameProp) && nameProp.GetString() is string s 
                    ? s 
                    : "No Title";

                // FIX: Explicitly define as nullable string? because abstract is optional
                string? description = doc.TryGetProperty("description", out var descProp) 
                    ? descProp.GetString() 
                    : null;
                
                var record = new MetadataRecord
                {
                    Title = title,
                    Abstract = description,
                    SourceFormat = MetadataFormat.JsonExpanded,
                    Authors = "System Generated"
                };

                return Result<MetadataRecord>.Success(record);
            }
            catch (Exception ex)
            {
                return Result<MetadataRecord>.Failure($"Failed to parse Expanded JSON: {ex.Message}");
            }
        }
    }
}