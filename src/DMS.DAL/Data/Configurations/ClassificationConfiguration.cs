using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class ClassificationConfiguration : IEntityTypeConfiguration<Classification>
{
    public void Configure(EntityTypeBuilder<Classification> builder)
    {
        builder.ToTable("Classifications");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Language).HasMaxLength(10);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.FullPath).HasMaxLength(1000);

        // Self-referencing hierarchy
        builder.HasOne<Classification>().WithMany().HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
        builder.HasIndex(e => e.Level);
    }
}
