using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.ToTable("Folders");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Path).HasMaxLength(2000);
        builder.Property(e => e.AccessMode).HasColumnType("tinyint");

        builder.HasOne<Cabinet>().WithMany().HasForeignKey(e => e.CabinetId);
        builder.HasOne<Folder>().WithMany().HasForeignKey(e => e.ParentFolderId);

        // Performance indexes
        builder.HasIndex(e => e.CabinetId);
        builder.HasIndex(e => e.ParentFolderId);
        builder.HasIndex(e => new { e.CabinetId, e.ParentFolderId });
    }
}
