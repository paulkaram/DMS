using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class LookupConfiguration : IEntityTypeConfiguration<Lookup>
{
    public void Configure(EntityTypeBuilder<Lookup> builder)
    {
        builder.ToTable("Lookups");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(1000);
    }
}

public class LookupItemConfiguration : IEntityTypeConfiguration<LookupItem>
{
    public void Configure(EntityTypeBuilder<LookupItem> builder)
    {
        builder.ToTable("LookupItems");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Value).HasMaxLength(512);
        builder.Property(e => e.DisplayText).HasMaxLength(512);
        builder.Property(e => e.Language).HasMaxLength(10);

        builder.HasOne<Lookup>().WithMany().HasForeignKey(e => e.LookupId);
    }
}
