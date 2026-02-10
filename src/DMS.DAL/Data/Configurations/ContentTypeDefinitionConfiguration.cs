using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class ContentTypeDefinitionConfiguration : IEntityTypeConfiguration<ContentTypeDefinition>
{
    public void Configure(EntityTypeBuilder<ContentTypeDefinition> builder)
    {
        builder.ToTable("ContentTypeDefinitions");
        builder.HasKey(e => e.Id);

        // Navigation
        builder.HasMany(e => e.Fields)
            .WithOne(e => e.ContentType)
            .HasForeignKey(e => e.ContentTypeId);
    }
}

public class ContentTypeFieldConfiguration : IEntityTypeConfiguration<ContentTypeField>
{
    public void Configure(EntityTypeBuilder<ContentTypeField> builder)
    {
        builder.ToTable("ContentTypeFields");
        builder.HasKey(e => e.Id);
    }
}

public class DocumentMetadataConfiguration : IEntityTypeConfiguration<DocumentMetadata>
{
    public void Configure(EntityTypeBuilder<DocumentMetadata> builder)
    {
        builder.ToTable("DocumentMetadata");
        builder.HasKey(e => e.Id);
    }
}

public class DocumentVersionMetadataConfiguration : IEntityTypeConfiguration<DocumentVersionMetadata>
{
    public void Configure(EntityTypeBuilder<DocumentVersionMetadata> builder)
    {
        builder.ToTable("DocumentVersionMetadata");
        builder.HasKey(e => e.Id);
    }
}

public class FolderContentTypeAssignmentConfiguration : IEntityTypeConfiguration<FolderContentTypeAssignment>
{
    public void Configure(EntityTypeBuilder<FolderContentTypeAssignment> builder)
    {
        builder.ToTable("FolderContentTypeAssignments");
        builder.HasKey(e => e.Id);

        // Ignore computed/joined properties
        builder.Ignore(e => e.ContentTypeName);
        builder.Ignore(e => e.ContentTypeDescription);
        builder.Ignore(e => e.ContentTypeIcon);
        builder.Ignore(e => e.ContentTypeColor);

        // Navigation
        builder.HasOne(e => e.ContentType)
            .WithMany()
            .HasForeignKey(e => e.ContentTypeId);
    }
}

public class CabinetContentTypeAssignmentConfiguration : IEntityTypeConfiguration<CabinetContentTypeAssignment>
{
    public void Configure(EntityTypeBuilder<CabinetContentTypeAssignment> builder)
    {
        builder.ToTable("CabinetContentTypeAssignments");
        builder.HasKey(e => e.Id);

        // Ignore computed/joined properties
        builder.Ignore(e => e.ContentTypeName);
        builder.Ignore(e => e.ContentTypeDescription);
        builder.Ignore(e => e.ContentTypeIcon);
        builder.Ignore(e => e.ContentTypeColor);

        // Navigation
        builder.HasOne(e => e.ContentType)
            .WithMany()
            .HasForeignKey(e => e.ContentTypeId);
    }
}
