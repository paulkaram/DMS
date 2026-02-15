using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class FolderTemplateConfiguration : IEntityTypeConfiguration<FolderTemplate>
{
    public void Configure(EntityTypeBuilder<FolderTemplate> builder)
    {
        builder.ToTable("FolderTemplates");
        builder.HasKey(e => e.Id);

        builder.Ignore(e => e.CreatedByName);
        builder.Ignore(e => e.UsageCount);

        builder.HasMany(e => e.Nodes).WithOne().HasForeignKey(e => e.TemplateId);
    }
}

public class FolderTemplateNodeConfiguration : IEntityTypeConfiguration<FolderTemplateNode>
{
    public void Configure(EntityTypeBuilder<FolderTemplateNode> builder)
    {
        builder.ToTable("FolderTemplateNodes");
        builder.HasKey(e => e.Id);

        builder.Ignore(e => e.ContentTypeName);
        builder.Ignore(e => e.Children);

        builder.HasOne<FolderTemplateNode>().WithMany().HasForeignKey(e => e.ParentNodeId);
    }
}

public class FolderTemplateUsageConfiguration : IEntityTypeConfiguration<FolderTemplateUsage>
{
    public void Configure(EntityTypeBuilder<FolderTemplateUsage> builder)
    {
        builder.ToTable("FolderTemplateUsage");
        builder.HasKey(e => e.Id);

        builder.Ignore(e => e.TemplateName);
        builder.Ignore(e => e.FolderName);
        builder.Ignore(e => e.AppliedByName);
    }
}
