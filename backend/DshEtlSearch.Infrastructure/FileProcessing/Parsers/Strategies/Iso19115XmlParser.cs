using System.Xml.Linq;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies
{
    public class Iso19115XmlParser : IMetadataParser
    {
        public async Task<Result<MetadataRecord>> ParseAsync(Stream content)
        {
            try
            {
                // Load XML asynchronously
                var xdoc = await XDocument.LoadAsync(content, LoadOptions.None, CancellationToken.None);
                
                // Define Namespaces typically found in ISO 19115
                XNamespace gmd = "http://www.isotc211.org/2005/gmd";
                XNamespace gco = "http://www.isotc211.org/2005/gco";

                // Extract Title (Defensive coding with null coalescing)
                var title = xdoc.Descendants(gmd + "title")
                                .Descendants(gco + "CharacterString")
                                .FirstOrDefault()?.Value 
                            ?? "Untitled Dataset";

                // Extract Abstract
                var abstractText = xdoc.Descendants(gmd + "abstract")
                                       .Descendants(gco + "CharacterString")
                                       .FirstOrDefault()?.Value;

                // Create Record
                var record = new MetadataRecord
                {
                    Title = title,
                    Abstract = abstractText,
                    SourceFormat = MetadataFormat.Iso19115Xml,
                    Authors = "Unknown", // XML parsing for authors is complex; simplified for MVP
                    Keywords = "iso, geospatial"
                };

                return Result<MetadataRecord>.Success(record);
            }
            catch (Exception ex)
            {
                return Result<MetadataRecord>.Failure($"Failed to parse ISO XML: {ex.Message}");
            }
        }
    }
}