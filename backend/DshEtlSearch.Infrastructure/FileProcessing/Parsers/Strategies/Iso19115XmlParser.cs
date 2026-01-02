using System.Xml.Linq;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies;

public class Iso19115XmlParser : IMetadataParser
{
    private static readonly XNamespace gmd = "http://www.isotc211.org/2005/gmd";
    private static readonly XNamespace gco = "http://www.isotc211.org/2005/gco";

    public Result<ParsedMetadataDto> Parse(Stream content)
    {
        try
        {
            var doc = XDocument.Load(content);

            var title = doc.Descendants(gmd + "title")
                            .Descendants(gco + "CharacterString")
                            .FirstOrDefault()?.Value 
                        ?? "Untitled Dataset";

            var abstractText = doc.Descendants(gmd + "abstract")
                                   .Descendants(gco + "CharacterString")
                                   .FirstOrDefault()?.Value 
                               ?? "No description available.";

            var url = doc.Descendants(gmd + "onLine")
                .Descendants(gmd + "linkage")
                .Descendants(gmd + "URL")
                .FirstOrDefault()?.Value;

            // FIX: Return the DTO instead of MetadataRecord entity
            var dto = new ParsedMetadataDto
            {
                Title = title,
                Abstract = abstractText,
                ResourceUrl = url,
                PublishedDate = DateTime.UtcNow // Ideally extract this from XML if possible
            };

            return Result<ParsedMetadataDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<ParsedMetadataDto>.Failure($"Failed to parse ISO XML: {ex.Message}");
        }
    }
}