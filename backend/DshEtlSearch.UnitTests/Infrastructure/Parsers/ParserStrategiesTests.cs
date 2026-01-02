using System.Text;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Infrastructure.FileProcessing.Parsers;
using DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies;
using FluentAssertions;
using Xunit;

namespace DshEtlSearch.Tests.Unit.Infrastructure.Parsers
{
    public class ParserStrategiesTests
    {
        // Helper to convert string to stream
        private Stream StringToStream(string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }

        [Fact]
        public void IsoXmlParser_ShouldExtractTitle_FromValidXml()
        {
            // Arrange
            var xml = @"<?xml version='1.0'?>
                        <gmd:MD_Metadata xmlns:gmd='http://www.isotc211.org/2005/gmd' xmlns:gco='http://www.isotc211.org/2005/gco'>
                            <gmd:identificationInfo>
                                <gmd:MD_DataIdentification>
                                    <gmd:citation>
                                        <gmd:CI_Citation>
                                            <gmd:title>
                                                <gco:CharacterString>ISO Test Title</gco:CharacterString>
                                            </gmd:title>
                                        </gmd:CI_Citation>
                                    </gmd:citation>
                                </gmd:MD_DataIdentification>
                            </gmd:identificationInfo>
                        </gmd:MD_Metadata>";
            
            var parser = new Iso19115XmlParser();

            // Act
            // FIX: Removed 'datasetId' parameter (Parser no longer needs it)
            var result = parser.Parse(StringToStream(xml));

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            
            // FIX: Check the DTO properties
            result.Value!.Title.Should().Be("ISO Test Title");
            
            // Note: We removed assertions for 'DatasetId' and 'SourceFormat' 
            // because the DTO is just a simple data bag, not the database record.
        }

        [Fact]
        public void JsonExpandedParser_ShouldExtractName()
        {
            // Arrange
            var json = @"{ ""name"": ""Expanded JSON Title"", ""description"": ""A test description"" }";
            var parser = new JsonExpandedParser();

            // Act
            // FIX: Removed 'datasetId' parameter
            var result = parser.Parse(StringToStream(json));

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("Expanded JSON Title");
            result.Value.Abstract.Should().Be("A test description");
        }

        [Fact]
        public void Factory_ShouldReturnCorrectParser()
        {
            // Arrange
            var factory = new MetadataParserFactory();

            // Act & Assert
            factory.GetParser(MetadataFormat.Iso19115Xml).Should().BeOfType<Iso19115XmlParser>();
            
            // Ensure RdfTurtleParser exists in your project
            factory.GetParser(MetadataFormat.RdfTurtle).Should().BeOfType<RdfTurtleParser>();
        }
    }
}