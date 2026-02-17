using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class StateTransitionRuleConfiguration : IEntityTypeConfiguration<StateTransitionRule>
{
    public void Configure(EntityTypeBuilder<StateTransitionRule> builder)
    {
        builder.ToTable("StateTransitionRules");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.RequiredRole).HasMaxLength(50);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => new { e.FromState, e.ToState })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        // Seed data for all valid transitions
        builder.HasData(
            // Draft -> Active (any user, requires classification)
            new StateTransitionRule
            {
                Id = Guid.Parse("a0000001-0000-0000-0000-000000000001"),
                FromState = DocumentState.Draft,
                ToState = DocumentState.Active,
                RequiresClassification = true,
                Description = "Activate draft document"
            },
            // Active -> Record (Records role, requires classification + retention)
            new StateTransitionRule
            {
                Id = Guid.Parse("a0000001-0000-0000-0000-000000000002"),
                FromState = DocumentState.Active,
                ToState = DocumentState.Record,
                RequiredRole = "Records",
                RequiresClassification = true,
                RequiresRetentionPolicy = true,
                MakesImmutable = true,
                Description = "Declare as official record"
            },
            // Record -> Active (Records role, requires approval)
            new StateTransitionRule
            {
                Id = Guid.Parse("a0000001-0000-0000-0000-000000000003"),
                FromState = DocumentState.Record,
                ToState = DocumentState.Active,
                RequiredRole = "Records",
                RequiresApproval = true,
                Description = "Reactivate record for correction"
            },
            // Record -> Archived (Records role, requires classification + retention)
            new StateTransitionRule
            {
                Id = Guid.Parse("a0000001-0000-0000-0000-000000000004"),
                FromState = DocumentState.Record,
                ToState = DocumentState.Archived,
                RequiredRole = "Records",
                RequiresClassification = true,
                RequiresRetentionPolicy = true,
                MakesImmutable = true,
                Description = "Archive record for long-term preservation"
            },
            // Archived -> PendingDisposal (Records role, requires approval)
            new StateTransitionRule
            {
                Id = Guid.Parse("a0000001-0000-0000-0000-000000000005"),
                FromState = DocumentState.Archived,
                ToState = DocumentState.PendingDisposal,
                RequiredRole = "Records",
                RequiresApproval = true,
                MakesImmutable = true,
                Description = "Initiate disposal process for archived record"
            },
            // PendingDisposal -> Disposed (Administrator role)
            new StateTransitionRule
            {
                Id = Guid.Parse("a0000001-0000-0000-0000-000000000006"),
                FromState = DocumentState.PendingDisposal,
                ToState = DocumentState.Disposed,
                RequiredRole = "Administrator",
                Description = "Execute disposal of record"
            },
            // PendingDisposal -> Active (Records role, cancel disposal)
            new StateTransitionRule
            {
                Id = Guid.Parse("a0000001-0000-0000-0000-000000000007"),
                FromState = DocumentState.PendingDisposal,
                ToState = DocumentState.Active,
                RequiredRole = "Records",
                Description = "Cancel disposal and reactivate"
            }
        );
    }
}

public class StateTransitionLogConfiguration : IEntityTypeConfiguration<StateTransitionLog>
{
    public void Configure(EntityTypeBuilder<StateTransitionLog> builder)
    {
        builder.ToTable("StateTransitionLogs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.Reason).HasMaxLength(1000);

        builder.HasIndex(e => e.DocumentId);
        builder.HasIndex(e => e.TransitionedAt);
    }
}
