using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class BackgroundJobExecutionConfiguration : IEntityTypeConfiguration<BackgroundJobExecution>
{
    public void Configure(EntityTypeBuilder<BackgroundJobExecution> builder)
    {
        builder.ToTable("BackgroundJobExecutions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");

        builder.Property(e => e.JobName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(4000);

        builder.HasIndex(e => e.JobName).HasDatabaseName("IX_BackgroundJobExecutions_JobName");
        builder.HasIndex(e => e.StartedAt).HasDatabaseName("IX_BackgroundJobExecutions_StartedAt");
    }
}
