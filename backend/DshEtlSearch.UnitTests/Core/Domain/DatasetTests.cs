using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using Xunit;

namespace DshEtlSearch.Tests.Unit.Core.Domain
{
    public class DatasetTests
    {
        [Fact]
        public void Constructor_ShouldInitializeIdAndDate()
        {
            // Arrange & Act
            // FIX: Updated to use the new constructor (Requires Title)
            var dataset = new Dataset("doi:10.1234/test", "Test Title");

            // Assert
            Assert.NotEqual(Guid.Empty, dataset.Id);
            Assert.Equal("doi:10.1234/test", dataset.FileIdentifier);
            Assert.Equal("Test Title", dataset.Title); // Verify Title is set
            Assert.True(dataset.IngestedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenIdentifierOrTitleMissing()
        {
            // Assert
            // Test missing ID
            Assert.Throws<ArgumentException>(() => new Dataset(null!, "Title"));
            
            // Test missing Title (Your new requirement)
            Assert.Throws<ArgumentException>(() => new Dataset("id", null!));
        }

        [Fact]
        public void AddDocument_ShouldAddDocumentToList_WithCorrectDetails()
        {
            // Arrange
            var dataset = new Dataset("doi:test", "Test Title");
            var fileName = "report.pdf";
            var size = 1024L;

            // FIX: Create the document object manually first
            // (Because you removed the helper method that took strings)
            var doc = new DataFile(dataset.Id, fileName);

            // Act
            dataset.AddDocument(doc);

            // Assert
            Assert.Single(dataset.SupportingDocuments); 
            
            var addedDoc = dataset.SupportingDocuments[0];
            Assert.Equal(fileName, addedDoc.FileName);
            Assert.Equal(dataset.Id, addedDoc.DatasetId); 
            Assert.NotNull(dataset.LastUpdated); // Check if LastUpdated was set
        }
    }
}