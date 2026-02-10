using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(e => e.Id);

        // Enum conversions
        builder.Property(e => e.NodeType).HasConversion<int>();
        builder.Property(e => e.PrincipalType).HasConversion<int>();
        builder.Property(e => e.PermissionLevel).HasConversion<int>();

        // Performance indexes (CRITICAL - hit on every request)
        builder.HasIndex(e => new { e.NodeType, e.NodeId });
        builder.HasIndex(e => new { e.PrincipalType, e.PrincipalId });
        builder.HasIndex(e => new { e.NodeType, e.NodeId, e.PrincipalType, e.PrincipalId });
    }
}
