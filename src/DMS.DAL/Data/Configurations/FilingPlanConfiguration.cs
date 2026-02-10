using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class FilingPlanConfiguration : IEntityTypeConfiguration<FilingPlan>
{
    public void Configure(EntityTypeBuilder<FilingPlan> builder)
    {
        builder.ToTable("FilingPlans");
        builder.HasKey(e => e.Id);

        // Ignore computed/joined properties
        builder.Ignore(e => e.FolderName);
        builder.Ignore(e => e.ClassificationName);
        builder.Ignore(e => e.DocumentTypeName);
    }
}
