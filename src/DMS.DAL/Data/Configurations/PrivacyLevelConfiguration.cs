using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class PrivacyLevelConfiguration : IEntityTypeConfiguration<PrivacyLevel>
{
    public void Configure(EntityTypeBuilder<PrivacyLevel> builder)
    {
        builder.ToTable("PrivacyLevels");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(256);
        builder.Property(e => e.Color).HasMaxLength(32);
        builder.Property(e => e.Description).HasMaxLength(1000);

        builder.HasIndex(e => e.Level).IsUnique();
    }
}
