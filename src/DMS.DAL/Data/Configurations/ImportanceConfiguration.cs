using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class ImportanceConfiguration : IEntityTypeConfiguration<Importance>
{
    public void Configure(EntityTypeBuilder<Importance> builder)
    {
        builder.ToTable("Importances");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(256);
        builder.Property(e => e.Color).HasMaxLength(32);
        builder.Property(e => e.Language).HasMaxLength(10);
    }
}
