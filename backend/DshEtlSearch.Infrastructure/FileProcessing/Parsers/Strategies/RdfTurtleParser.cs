using System.Text.RegularExpressions;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies
{
    public class RdfTurtleParser : IMetadataParser
    {
        public async Task<Result<MetadataRecord>> ParseAsync(Stream content)
        {
            try
            {
                using var reader = new StreamReader(content);
                var text = await reader.ReadToEndAsync();

                // Simple Regex to find Dublin Core Title (dct:title "Some Title")
                var titleMatch = Regex.Match(text, @"dct:title\s+""(.*?)""");
                var descMatch = Regex.Match(text, @"dct:description\s+""(.*?)""");

                var record = new MetadataRecord
                {
                    Title = titleMatch.Success ? titleMatch.Groups[1].Value : "Unknown RDF Dataset",
                    Abstract = descMatch.Success ? descMatch.Groups[1].Value : null,
                    SourceFormat = MetadataFormat.RdfTurtle
                };

                return Result<MetadataRecord>.Success(record);
            }
            catch (Exception ex)
            {
                return Result<MetadataRecord>.Failure($"RDF Parse Error: {ex.Message}");
            }
        }
    }
}