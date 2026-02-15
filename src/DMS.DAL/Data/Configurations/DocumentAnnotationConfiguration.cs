using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DocumentAnnotationConfiguration : IEntityTypeConfiguration<DocumentAnnotation>
{
    public void Configure(EntityTypeBuilder<DocumentAnnotation> builder)
    {
        builder.ToTable("DocumentAnnotations");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.DocumentId);
        builder.HasIndex(e => e.CreatedBy);

        // Ignore computed/display properties
        builder.Ignore(e => e.CreatedByName);
    }
}

public class SavedSignatureConfiguration : IEntityTypeConfiguration<SavedSignature>
{
    public void Configure(EntityTypeBuilder<SavedSignature> builder)
    {
        builder.ToTable("SavedSignatures");
        builder.HasKey(e => e.Id);
    }
}
