using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DisposalCertificateConfiguration : IEntityTypeConfiguration<DisposalCertificate>
{
    public void Configure(EntityTypeBuilder<DisposalCertificate> builder)
    {
        builder.ToTable("DisposalCertificates");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.RetentionPolicyId);

        builder.Ignore(e => e.DisposedByName);
        builder.Ignore(e => e.ApprovedByName);
    }
}

public class LegalHoldConfiguration : IEntityTypeConfiguration<LegalHold>
{
    public void Configure(EntityTypeBuilder<LegalHold> builder)
    {
        builder.ToTable("LegalHolds");
        builder.HasKey(e => e.Id);
    }
}

public class LegalHoldDocumentConfiguration : IEntityTypeConfiguration<LegalHoldDocument>
{
    public void Configure(EntityTypeBuilder<LegalHoldDocument> builder)
    {
        builder.ToTable("LegalHoldDocuments");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.LegalHoldId);
        builder.HasIndex(e => e.DocumentId);

        builder.Ignore(e => e.DocumentName);
        builder.Ignore(e => e.HoldName);
    }
}

public class IntegrityVerificationLogConfiguration : IEntityTypeConfiguration<IntegrityVerificationLog>
{
    public void Configure(EntityTypeBuilder<IntegrityVerificationLog> builder)
    {
        builder.ToTable("IntegrityVerificationLogs");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.DocumentId);
    }
}

public class PreservationMetadataConfiguration : IEntityTypeConfiguration<PreservationMetadata>
{
    public void Configure(EntityTypeBuilder<PreservationMetadata> builder)
    {
        builder.ToTable("PreservationMetadata");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.DocumentId);
    }
}
