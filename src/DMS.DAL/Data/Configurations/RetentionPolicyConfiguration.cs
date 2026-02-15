using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("RetentionPolicies");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.FolderId);

        // Ignore computed/joined properties
        builder.Ignore(e => e.FolderName);
        builder.Ignore(e => e.ClassificationName);
        builder.Ignore(e => e.DocumentTypeName);
    }
}

public class DocumentRetentionConfiguration : IEntityTypeConfiguration<DocumentRetention>
{
    public void Configure(EntityTypeBuilder<DocumentRetention> builder)
    {
        builder.ToTable("DocumentRetentions");
        builder.HasKey(e => e.Id);

        // Ignore computed/joined properties
        builder.Ignore(e => e.DocumentName);
        builder.Ignore(e => e.PolicyName);

        // Performance indexes
        builder.HasIndex(e => e.DocumentId);
        builder.HasIndex(e => new { e.Status, e.ExpirationDate });
    }
}
