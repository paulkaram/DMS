using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class FolderLinkConfiguration : IEntityTypeConfiguration<FolderLink>
{
    public void Configure(EntityTypeBuilder<FolderLink> builder)
    {
        builder.ToTable("FolderLinks");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.SourceFolderId);
        builder.HasIndex(e => e.TargetFolderId);

        // Ignore computed/joined properties
        builder.Ignore(e => e.SourceFolderName);
        builder.Ignore(e => e.TargetFolderName);
        builder.Ignore(e => e.TargetFolderPath);
    }
}
