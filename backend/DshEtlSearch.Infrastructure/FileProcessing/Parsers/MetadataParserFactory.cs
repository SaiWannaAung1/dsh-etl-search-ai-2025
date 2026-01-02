using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers;

// FIX: Added ": IMetadataParserFactory"
public class MetadataParserFactory : IMetadataParserFactory
{
    public IMetadataParser GetParser(MetadataFormat format)
    {
        return format switch
        {
            MetadataFormat.Iso19115Xml => new Iso19115XmlParser(),
            MetadataFormat.JsonExpanded => new JsonExpandedParser(),
            MetadataFormat.SchemaOrgJsonLd => new SchemaOrgJsonLdParser(),
            MetadataFormat.RdfTurtle => new RdfTurtleParser(),
            _ => throw new ArgumentException($"No parser strategy found for format: {format}")
        };
    }
}