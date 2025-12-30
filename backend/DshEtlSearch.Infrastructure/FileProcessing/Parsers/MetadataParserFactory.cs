using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies;

namespace DshEtlSearch.Infrastructure.FileProcessing.Parsers
{
    public class MetadataParserFactory
    {
        // In a real DI scenario, we might inject IServiceProvider, 
        // but for simplicity and clarity, we instantiate strategies here.
        
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
}