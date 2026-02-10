using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class SystemActionConfiguration : IEntityTypeConfiguration<SystemAction>
{
    public void Configure(EntityTypeBuilder<SystemAction> builder)
    {
        builder.ToTable("SystemActions");
        builder.HasKey(e => e.Id);
    }
}

public class RoleActionPermissionConfiguration : IEntityTypeConfiguration<RoleActionPermission>
{
    public void Configure(EntityTypeBuilder<RoleActionPermission> builder)
    {
        builder.ToTable("RoleActionPermissions");
        builder.HasKey(e => e.Id);

        builder.Ignore(e => e.RoleName);
        builder.Ignore(e => e.ActionCode);
        builder.Ignore(e => e.ActionName);
        builder.Ignore(e => e.ActionCategory);
    }
}
