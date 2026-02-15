using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DocumentAttachmentConfiguration : IEntityTypeConfiguration<DocumentAttachment>
{
    public void Configure(EntityTypeBuilder<DocumentAttachment> builder)
    {
        builder.ToTable("DocumentAttachments");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.DocumentId);
        builder.HasIndex(e => e.CreatedBy);

        // Ignore computed/display properties
        builder.Ignore(e => e.CreatedByName);
    }
}
