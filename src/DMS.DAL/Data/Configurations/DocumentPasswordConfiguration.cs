using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DocumentPasswordConfiguration : IEntityTypeConfiguration<DocumentPassword>
{
    public void Configure(EntityTypeBuilder<DocumentPassword> builder)
    {
        builder.ToTable("DocumentPasswords");
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.DocumentId);
    }
}
