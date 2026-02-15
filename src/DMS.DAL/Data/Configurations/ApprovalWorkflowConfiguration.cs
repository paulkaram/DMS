using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        builder.ToTable("ApprovalWorkflows");
        builder.HasKey(e => e.Id);

        // Ignore computed/joined properties
        builder.Ignore(e => e.FolderName);

        builder.Property(e => e.TriggerType).HasMaxLength(50).HasDefaultValue("Manual");
        builder.Property(e => e.InheritToSubfolders).HasDefaultValue(true);

        // Navigation
        builder.HasMany(e => e.Steps)
            .WithOne()
            .HasForeignKey(e => e.WorkflowId);
    }
}

public class ApprovalWorkflowStepConfiguration : IEntityTypeConfiguration<ApprovalWorkflowStep>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowStep> builder)
    {
        builder.ToTable("ApprovalWorkflowSteps");
        builder.HasKey(e => e.Id);

        // Ignore computed/joined properties
        builder.Ignore(e => e.ApproverUserName);
        builder.Ignore(e => e.ApproverRoleName);
        builder.Ignore(e => e.ApproverStructureName);
        builder.Ignore(e => e.StatusName);
        builder.Ignore(e => e.StatusColor);

        builder.Property(e => e.AssignToManager).HasDefaultValue(false);
    }
}
