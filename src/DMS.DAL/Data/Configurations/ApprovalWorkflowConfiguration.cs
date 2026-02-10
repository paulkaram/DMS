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
    }
}
