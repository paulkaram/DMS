using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DocumentLinkConfiguration : IEntityTypeConfiguration<DocumentLink>
{
    public void Configure(EntityTypeBuilder<DocumentLink> builder)
    {
        builder.ToTable("DocumentLinks");
        builder.HasKey(e => e.Id);

        // Ignore computed/display properties
        builder.Ignore(e => e.SourceDocumentName);
        builder.Ignore(e => e.TargetDocumentName);
        builder.Ignore(e => e.TargetDocumentPath);
        builder.Ignore(e => e.CreatedByName);
    }
}
