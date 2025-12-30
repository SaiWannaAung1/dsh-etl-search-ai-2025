using DshEtlSearch.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace DshEtlSearch.Infrastructure.Data.SQLite
{
    /// <summary>
    /// Represents the session with the SQLite database.
    /// Configures the schema and relationships for Entity Framework Core.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<Dataset> Datasets { get; set; }
        public DbSet<MetadataRecord> MetadataRecords { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configure Dataset (Root Aggregate)
            modelBuilder.Entity<Dataset>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SourceIdentifier).IsUnique(); // Ensure DOIs are unique
                entity.Property(e => e.SourceIdentifier).IsRequired();
            });

            // 2. Configure 1:1 Relationship (Dataset <-> MetadataRecord)
            modelBuilder.Entity<Dataset>()
                .HasOne(d => d.Metadata)
                .WithOne()
                .HasForeignKey<MetadataRecord>(m => m.DatasetId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting Dataset deletes Metadata

            // 3. Configure 1:N Relationship (Dataset <-> SupportingDocuments)
            modelBuilder.Entity<Dataset>()
                .HasMany(d => d.Documents)
                .WithOne()
                .HasForeignKey(doc => doc.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);

            // 4. Ignore EmbeddingVectors in SQLite (They go to Vector Store/Qdrant)
            modelBuilder.Entity<Dataset>()
                .Ignore(d => d.Embeddings);
        }
    }
}