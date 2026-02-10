using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(e => e.Id);

        // Relationships (no nav properties, just foreign keys)
        builder.HasOne<Folder>().WithMany().HasForeignKey(e => e.FolderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Classification>().WithMany().HasForeignKey(e => e.ClassificationId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Importance>().WithMany().HasForeignKey(e => e.ImportanceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<DocumentType>().WithMany().HasForeignKey(e => e.DocumentTypeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<ContentTypeDefinition>().WithMany().HasForeignKey(e => e.ContentTypeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<RetentionPolicy>().WithMany().HasForeignKey(e => e.RetentionPolicyId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<LegalHold>().WithMany().HasForeignKey(e => e.LegalHoldId).OnDelete(DeleteBehavior.SetNull);

        // Performance indexes
        builder.HasIndex(e => e.FolderId);
        builder.HasIndex(e => e.CreatedBy);
        builder.HasIndex(e => new { e.FolderId, e.IsActive });
        builder.HasIndex(e => new { e.CreatedBy, e.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(e => e.Name);
    }
}
