using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class StructureConfiguration : IEntityTypeConfiguration<Structure>
{
    public void Configure(EntityTypeBuilder<Structure> builder)
    {
        builder.ToTable("Structures");
        builder.HasKey(e => e.Id);

        // Enum conversion
        builder.Property(e => e.StructureType).HasConversion<int>();

        // Self-referencing relationship
        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId);

        // Structure members
        builder.HasMany(e => e.Members)
            .WithOne(e => e.Structure)
            .HasForeignKey(e => e.StructureId);

        // Performance indexes
        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.StructureType);
    }
}

public class StructureMemberConfiguration : IEntityTypeConfiguration<StructureMember>
{
    public void Configure(EntityTypeBuilder<StructureMember> builder)
    {
        builder.ToTable("StructureMembers");
        builder.HasKey(e => e.Id);

        // Navigation to User
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);

        // Performance indexes
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.StructureId);
        builder.HasIndex(e => new { e.UserId, e.StructureId });
    }
}

public class EffectivePermissionConfiguration : IEntityTypeConfiguration<EffectivePermission>
{
    public void Configure(EntityTypeBuilder<EffectivePermission> builder)
    {
        builder.ToTable("EffectivePermissions");
        builder.HasKey(e => e.Id);

        // Enum conversions
        builder.Property(e => e.NodeType).HasConversion<int>();
        builder.Property(e => e.EffectiveLevel).HasConversion<int>();
        builder.Property(e => e.SourceNodeType).HasConversion<int?>();

        // Performance indexes (CRITICAL)
        builder.HasIndex(e => new { e.NodeType, e.NodeId, e.UserId });
        builder.HasIndex(e => e.UserId);
    }
}

public class PermissionAuditLogConfiguration : IEntityTypeConfiguration<PermissionAuditLog>
{
    public void Configure(EntityTypeBuilder<PermissionAuditLog> builder)
    {
        builder.ToTable("PermissionAuditLogs");
        builder.HasKey(e => e.Id);

        // Enum conversions
        builder.Property(e => e.NodeType).HasConversion<int>();
        builder.Property(e => e.PrincipalType).HasConversion<int>();
    }
}

public class PermissionDelegationConfiguration : IEntityTypeConfiguration<PermissionDelegation>
{
    public void Configure(EntityTypeBuilder<PermissionDelegation> builder)
    {
        builder.ToTable("PermissionDelegations");
        builder.HasKey(e => e.Id);

        // Enum conversions
        builder.Property(e => e.NodeType).HasConversion<int?>();
        builder.Property(e => e.PermissionLevel).HasConversion<int>();

        // Navigation properties
        builder.HasOne(e => e.Delegator)
            .WithMany()
            .HasForeignKey(e => e.DelegatorId);

        builder.HasOne(e => e.Delegate)
            .WithMany()
            .HasForeignKey(e => e.DelegateId);

        // Performance indexes
        builder.HasIndex(e => e.DelegatorId);
        builder.HasIndex(e => e.DelegateId);
    }
}
