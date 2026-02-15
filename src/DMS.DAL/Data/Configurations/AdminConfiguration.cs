using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.ToTable("Bookmarks");
        builder.HasKey(e => e.Id);
    }
}

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.ToTable("Cases");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.FolderId);

        builder.Ignore(e => e.AssignedToUserName);
        builder.Ignore(e => e.FolderName);
    }
}

public class ServiceEndpointConfiguration : IEntityTypeConfiguration<ServiceEndpoint>
{
    public void Configure(EntityTypeBuilder<ServiceEndpoint> builder)
    {
        builder.ToTable("ServiceEndpoints");
        builder.HasKey(e => e.Id);
    }
}

public class ExportConfigConfiguration : IEntityTypeConfiguration<ExportConfig>
{
    public void Configure(EntityTypeBuilder<ExportConfig> builder)
    {
        builder.ToTable("ExportConfigs");
        builder.HasKey(e => e.Id);
    }
}

public class NamingConventionConfiguration : IEntityTypeConfiguration<NamingConvention>
{
    public void Configure(EntityTypeBuilder<NamingConvention> builder)
    {
        builder.ToTable("NamingConventions");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.FolderId);

        builder.Ignore(e => e.FolderName);
        builder.Ignore(e => e.DocumentTypeName);
    }
}

public class OrganizationTemplateConfiguration : IEntityTypeConfiguration<OrganizationTemplate>
{
    public void Configure(EntityTypeBuilder<OrganizationTemplate> builder)
    {
        builder.ToTable("OrganizationTemplates");
        builder.HasKey(e => e.Id);
    }
}

public class PermissionLevelDefinitionConfiguration : IEntityTypeConfiguration<PermissionLevelDefinition>
{
    public void Configure(EntityTypeBuilder<PermissionLevelDefinition> builder)
    {
        builder.ToTable("PermissionLevelDefinitions");
        builder.HasKey(e => e.Id);
    }
}

public class PurposeConfiguration : IEntityTypeConfiguration<Purpose>
{
    public void Configure(EntityTypeBuilder<Purpose> builder)
    {
        builder.ToTable("Purposes");
        builder.HasKey(e => e.Id);
    }
}

public class ScanConfigConfiguration : IEntityTypeConfiguration<ScanConfig>
{
    public void Configure(EntityTypeBuilder<ScanConfig> builder)
    {
        builder.ToTable("ScanConfigs");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.TargetFolderId);

        builder.Ignore(e => e.TargetFolderName);
    }
}

public class SearchConfigConfiguration : IEntityTypeConfiguration<SearchConfig>
{
    public void Configure(EntityTypeBuilder<SearchConfig> builder)
    {
        builder.ToTable("SearchConfigs");
        builder.HasKey(e => e.Id);
    }
}
