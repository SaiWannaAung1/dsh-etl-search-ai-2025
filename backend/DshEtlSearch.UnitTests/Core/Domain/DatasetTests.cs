using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using Xunit;

namespace DshEtlSearch.UnitTests.Core.Domain
{
    public class DatasetTests
    {
        [Fact]
        public void Constructor_ShouldInitializeIdAndDate()
        {
            // Arrange & Act
            var dataset = new Dataset("doi:10.1234/test");

            // Assert
            Assert.NotEqual(Guid.Empty, dataset.Id);
            Assert.Equal("doi:10.1234/test", dataset.SourceIdentifier);
            Assert.True(dataset.IngestedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenIdentifierIsNull()
        {
            // Assert
            // FIX: Use 'null!' to tell the compiler we are intentionally testing invalid null input
            Assert.Throws<ArgumentNullException>(() => new Dataset(null!));
        }

        [Fact]
        public void AddDocument_ShouldAddDocumentToList_WithCorrectDetails()
        {
            // Arrange
            var dataset = new Dataset("doi:test");
            var fileName = "report.pdf";
            var size = 1024L;

            // Act
            dataset.AddDocument(fileName, FileType.Pdf, size);

            // Assert
            Assert.Single(dataset.Documents); // List should have 1 item
            
            var doc = dataset.Documents[0];
            Assert.Equal(fileName, doc.FileName);
            Assert.Equal(FileType.Pdf, doc.Type);
            Assert.Equal(size, doc.SizeBytes);
            Assert.Equal(dataset.Id, doc.DatasetId); // Verify Foreign Key link
        }
    }
}