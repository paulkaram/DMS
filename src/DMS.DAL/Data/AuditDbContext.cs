using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Data;

/// <summary>
/// Separate DbContext for audit trail data (NCA/NCAR compliance).
/// Stored in a dedicated DMS_Audit database for tamper isolation,
/// independent retention, and regulatory separation.
/// </summary>
public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<PermissionAuditLog> PermissionAuditLogs => Set<PermissionAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ActivityLog configuration
        modelBuilder.Entity<ActivityLog>(builder =>
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
        });

        // PermissionAuditLog configuration
        modelBuilder.Entity<PermissionAuditLog>(builder =>
        {
            builder.ToTable("PermissionAuditLogs");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.NodeType).HasConversion<int>();
            builder.Property(e => e.PrincipalType).HasConversion<int>();
        });
    }
}
