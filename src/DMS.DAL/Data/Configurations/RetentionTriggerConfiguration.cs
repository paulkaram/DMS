using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class RetentionTriggerEventConfiguration : IEntityTypeConfiguration<RetentionTriggerEvent>
{
    public void Configure(EntityTypeBuilder<RetentionTriggerEvent> builder)
    {
        builder.ToTable("RetentionTriggerEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.TriggerFieldName).HasMaxLength(200);
        builder.Property(e => e.TriggerFieldValue).HasMaxLength(500);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.RetentionPolicyId);
    }
}

public class RetentionTriggerLogConfiguration : IEntityTypeConfiguration<RetentionTriggerLog>
{
    public void Configure(EntityTypeBuilder<RetentionTriggerLog> builder)
    {
        builder.ToTable("RetentionTriggerLogs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");

        builder.HasIndex(e => e.DocumentId);
        builder.HasIndex(e => e.RetentionPolicyId);
    }
}
