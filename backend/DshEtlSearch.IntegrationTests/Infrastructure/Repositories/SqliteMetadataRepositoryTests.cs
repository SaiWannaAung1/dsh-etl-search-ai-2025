using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Infrastructure.Data.SQLite;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DshEtlSearch.IntegrationTests.Infrastructure.Repositories
{
    // Helper Specification for Testing
    public class TestDatasetWithChildrenSpec : BaseSpecification<Dataset>
    {
        public TestDatasetWithChildrenSpec(Guid id) : base(d => d.Id == id)
        {
            AddInclude("Metadata");
            AddInclude("Documents");
        }
    }

    public class SqliteMetadataRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly SqliteMetadataRepository _repository;

        public SqliteMetadataRepositoryTests()
        {
            // 1. Setup In-Memory SQLite
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            _repository = new SqliteMetadataRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldPersist_And_GetEntityWithSpec_ShouldRetrieveIncludes()
        {
            // Arrange
            var dataset = new Dataset("doi:spec-test");
            dataset.Metadata = new MetadataRecord 
            { 
                Title = "Spec Test", 
                // Fix: Providing required fields (nullable or not, good to be safe)
                Authors = "Test Author",
                SourceFormat = MetadataFormat.Iso19115Xml 
            };
            dataset.AddDocument("doc.pdf", FileType.Pdf, 123);

            // Act
            await _repository.AddAsync(dataset);
            await _repository.SaveChangesAsync();
            
            // Clear tracker to force DB fetch
            _context.ChangeTracker.Clear();

            // Act 2: Use Specification
            var spec = new TestDatasetWithChildrenSpec(dataset.Id);
            var result = await _repository.GetEntityWithSpec(spec);

            // Assert
            result.Should().NotBeNull();
            result!.Metadata.Should().NotBeNull(); // Verify Include worked
            result.Metadata.Title.Should().Be("Spec Test");
            result.Documents.Should().HaveCount(1); // Verify Include worked
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenDatasetExists()
        {
            // Arrange
            var dataset = new Dataset("doi:exists-check");
            // Minimal required data
            dataset.Metadata = new MetadataRecord { Title = "T", Authors = "A" }; 
            
            await _repository.AddAsync(dataset);
            await _repository.SaveChangesAsync();

            // Act
            var exists = await _repository.ExistsAsync("doi:exists-check");
            var notExists = await _repository.ExistsAsync("doi:fake");

            // Assert
            exists.Should().BeTrue();
            notExists.Should().BeFalse();
        }

        public void Dispose()
        {
            _connection.Close();
            _context.Dispose();
        }
    }
}