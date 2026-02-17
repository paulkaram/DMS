using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DisposalRequestConfiguration : IEntityTypeConfiguration<DisposalRequest>
{
    public void Configure(EntityTypeBuilder<DisposalRequest> builder)
    {
        builder.ToTable("DisposalRequests");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.RequestNumber).HasMaxLength(50);
        builder.Property(e => e.BatchReference).HasMaxLength(100);
        builder.Property(e => e.DisposalMethod).HasMaxLength(50);
        builder.Property(e => e.Reason).HasMaxLength(1000);
        builder.Property(e => e.LegalBasis).HasMaxLength(500);

        builder.HasIndex(e => e.RequestNumber).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.RequestedAt);
    }
}

public class DisposalRequestDocumentConfiguration : IEntityTypeConfiguration<DisposalRequestDocument>
{
    public void Configure(EntityTypeBuilder<DisposalRequestDocument> builder)
    {
        builder.ToTable("DisposalRequestDocuments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(e => e.DisposalRequestId);
        builder.HasIndex(e => e.DocumentId);

        builder.Ignore(e => e.DocumentName);
    }
}

public class DisposalApprovalConfiguration : IEntityTypeConfiguration<DisposalApproval>
{
    public void Configure(EntityTypeBuilder<DisposalApproval> builder)
    {
        builder.ToTable("DisposalApprovals");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.Comments).HasMaxLength(1000);

        builder.HasIndex(e => e.DisposalRequestId);
        builder.HasIndex(e => new { e.DisposalRequestId, e.ApprovalLevel });
    }
}
