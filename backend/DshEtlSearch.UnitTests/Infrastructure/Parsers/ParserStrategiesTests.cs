using System.Text;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Infrastructure.FileProcessing.Parsers;
using DshEtlSearch.Infrastructure.FileProcessing.Parsers.Strategies;
using FluentAssertions;
using Xunit;

namespace DshEtlSearch.UnitTests.Infrastructure.Parsers
{
    public class ParserStrategiesTests
    {
        private Stream StringToStream(string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }

        [Fact]
        public async Task IsoXmlParser_ShouldExtractTitle_FromValidXml()
        {
            // Arrange
            var xml = @"<?xml version='1.0'?>
                        <gmd:MD_Metadata xmlns:gmd='http://www.isotc211.org/2005/gmd' xmlns:gco='http://www.isotc211.org/2005/gco'>
                            <gmd:identificationInfo>
                                <gmd:title>
                                    <gco:CharacterString>ISO Test Title</gco:CharacterString>
                                </gmd:title>
                            </gmd:identificationInfo>
                        </gmd:MD_Metadata>";
            var parser = new Iso19115XmlParser();

            // Act
            var result = await parser.ParseAsync(StringToStream(xml));

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("ISO Test Title");
            result.Value.SourceFormat.Should().Be(MetadataFormat.Iso19115Xml);
        }

        [Fact]
        public async Task JsonExpandedParser_ShouldExtractName()
        {
            // Arrange
            var json = @"{ ""name"": ""Expanded JSON Title"", ""description"": ""A test description"" }";
            var parser = new JsonExpandedParser();

            // Act
            var result = await parser.ParseAsync(StringToStream(json));

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Title.Should().Be("Expanded JSON Title");
        }

        [Fact]
        public async Task Factory_ShouldReturnCorrectParser()
        {
            // Arrange
            var factory = new MetadataParserFactory();

            // Act & Assert
            factory.GetParser(MetadataFormat.Iso19115Xml).Should().BeOfType<Iso19115XmlParser>();
            factory.GetParser(MetadataFormat.RdfTurtle).Should().BeOfType<RdfTurtleParser>();
        }
    }
}