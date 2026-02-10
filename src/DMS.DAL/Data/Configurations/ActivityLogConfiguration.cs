using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action).HasMaxLength(256);
        builder.Property(e => e.NodeType).HasConversion<int?>();
        builder.Property(e => e.NodeName).HasMaxLength(512);
        builder.Property(e => e.Details).HasMaxLength(2000);
        builder.Property(e => e.UserName).HasMaxLength(256);
        builder.Property(e => e.IpAddress).HasMaxLength(64);

        // Performance indexes
        builder.HasIndex(e => e.CreatedAt).IsDescending(true);
        builder.HasIndex(e => new { e.UserId, e.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(e => new { e.NodeType, e.NodeId });
    }
}
