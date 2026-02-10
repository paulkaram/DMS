using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class RecycleBinConfiguration : IEntityTypeConfiguration<RecycleBinItem>
{
    public void Configure(EntityTypeBuilder<RecycleBinItem> builder)
    {
        builder.ToTable("RecycleBin");
        builder.HasKey(e => e.Id);

        builder.Ignore(e => e.DeletedByUserName);

        // Performance indexes
        builder.HasIndex(e => e.DeletedBy);
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => new { e.DeletedBy, e.DeletedAt }).IsDescending(false, true);
    }
}
