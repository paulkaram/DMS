using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class SearchIndexQueueConfiguration : IEntityTypeConfiguration<SearchIndexQueue>
{
    public void Configure(EntityTypeBuilder<SearchIndexQueue> builder)
    {
        builder.ToTable("SearchIndexQueue");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");

        builder.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(e => e.QueuedAt)
            .HasFilter("ProcessedAt IS NULL")
            .HasDatabaseName("IX_SearchIndexQueue_Unprocessed");

        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_SearchIndexQueue_Entity");
    }
}
