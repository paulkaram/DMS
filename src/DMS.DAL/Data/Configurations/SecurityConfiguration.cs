using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class EncryptionKeyStoreConfiguration : IEntityTypeConfiguration<EncryptionKeyStore>
{
    public void Configure(EntityTypeBuilder<EncryptionKeyStore> builder)
    {
        builder.ToTable("EncryptionKeyStore");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.WrappedKey).HasMaxLength(1024).IsRequired();
        builder.Property(e => e.KeyAlgorithm).HasMaxLength(50);

        builder.HasIndex(e => new { e.DocumentId, e.KeyVersion }).IsUnique();
    }
}

public class AccessReviewCampaignConfiguration : IEntityTypeConfiguration<AccessReviewCampaign>
{
    public void Configure(EntityTypeBuilder<AccessReviewCampaign> builder)
    {
        builder.ToTable("AccessReviewCampaigns");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);

        builder.HasIndex(e => e.Status);
    }
}

public class AccessReviewEntryConfiguration : IEntityTypeConfiguration<AccessReviewEntry>
{
    public void Configure(EntityTypeBuilder<AccessReviewEntry> builder)
    {
        builder.ToTable("AccessReviewEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.NodeType).HasMaxLength(50);
        builder.Property(e => e.PermissionSource).HasMaxLength(200);
        builder.Property(e => e.Comments).HasMaxLength(500);

        builder.HasIndex(e => e.CampaignId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Decision);
    }
}
