using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Infrastructure.Data.SQLite;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DshEtlSearch.IntegrationTests.Infrastructure.Repositories
{
    public class SqliteMetadataRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly SqliteMetadataRepository _repository;

        public SqliteMetadataRepositoryTests()
        {
            // 1. Set up In-Memory SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // 2. Configure EF Core to use this connection
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            // 3. Create Schema
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            _repository = new SqliteMetadataRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldPersistDataset_WithRelationships()
        {
            // Arrange
            var dataset = new Dataset("doi:10.1000/test-data");
    
            // Add Child: Metadata
            dataset.Metadata = new MetadataRecord
            {
                Title = "Integration Test Title",
                Abstract = "Testing persistence...",
                Authors = "Dr. Test Author, Prof. Data", 
                Keywords = "integration, testing, sqlite",
                SourceFormat = MetadataFormat.Iso19115Xml
            };

            // Add Child: Document
            dataset.AddDocument("report.pdf", FileType.Pdf, 500);
            

            // Act
            await _repository.AddAsync(dataset);
            await _repository.SaveChangesAsync();

            // Assert (Clear tracker to ensure we fetch from DB, not memory cache)
            _context.ChangeTracker.Clear();
            
            var fetched = await _repository.GetByIdAsync(dataset.Id);

            fetched.Should().NotBeNull();
            fetched!.SourceIdentifier.Should().Be("doi:10.1000/test-data");
            fetched.Metadata.Should().NotBeNull();
            fetched.Metadata.Title.Should().Be("Integration Test Title");
            fetched.Documents.Should().HaveCount(1);
            fetched.Documents[0].FileName.Should().Be("report.pdf");
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenDatasetExists()
        {
            // Arrange
            var dataset = new Dataset("doi:existing");
            await _repository.AddAsync(dataset);
            await _repository.SaveChangesAsync();

            // Act
            var exists = await _repository.ExistsAsync("doi:existing");

            // Assert
            exists.Should().BeTrue();
        }

        public void Dispose()
        {
            _connection.Close();
            _context.Dispose();
        }
    }
}