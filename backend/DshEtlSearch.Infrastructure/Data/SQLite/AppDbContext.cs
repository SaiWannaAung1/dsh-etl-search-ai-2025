using DshEtlSearch.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace DshEtlSearch.Infrastructure.Data.SQLite
{
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

            // 1. Configure Dataset
            modelBuilder.Entity<Dataset>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasIndex(e => e.FileIdentifier).IsUnique(); 
                entity.Property(e => e.FileIdentifier).IsRequired();
                
                // Ignore Embeddings for SQLite (they go to Vector DB)
                entity.Ignore(d => d.Embeddings);
            });

            // 2. Configure 1:N Relationship (Dataset <-> MetadataRecords)
            // FIX: Changed from HasOne (1:1) to HasMany (1:N)
            modelBuilder.Entity<Dataset>()
                .HasMany(d => d.MetadataRecords) // Use the List property
                .WithOne(m => m.Dataset)         // Link back to Parent
                .HasForeignKey(m => m.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Configure 1:N Relationship (Dataset <-> SupportingDocuments)
            modelBuilder.Entity<Dataset>()
                .HasMany(d => d.SupportingDocuments)
                .WithOne()
                .HasForeignKey(doc => doc.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}