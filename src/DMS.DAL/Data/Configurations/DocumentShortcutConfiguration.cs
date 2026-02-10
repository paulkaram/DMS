using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DocumentShortcutConfiguration : IEntityTypeConfiguration<DocumentShortcut>
{
    public void Configure(EntityTypeBuilder<DocumentShortcut> builder)
    {
        builder.ToTable("DocumentShortcuts");
        builder.HasKey(e => e.Id);

        // Ignore computed/display properties
        builder.Ignore(e => e.DocumentName);
        builder.Ignore(e => e.FolderName);
        builder.Ignore(e => e.FolderPath);

        // Performance indexes
        builder.HasIndex(e => e.FolderId);
        builder.HasIndex(e => e.DocumentId);
    }
}
