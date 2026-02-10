using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class PatternConfiguration : IEntityTypeConfiguration<Pattern>
{
    public void Configure(EntityTypeBuilder<Pattern> builder)
    {
        builder.ToTable("Patterns");
        builder.HasKey(e => e.Id);

        // Ignore computed/joined properties
        builder.Ignore(e => e.TargetFolderName);
        builder.Ignore(e => e.ContentTypeName);
        builder.Ignore(e => e.ClassificationName);
        builder.Ignore(e => e.DocumentTypeName);
    }
}
