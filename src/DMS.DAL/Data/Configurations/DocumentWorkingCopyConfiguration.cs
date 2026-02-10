using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DocumentWorkingCopyConfiguration : IEntityTypeConfiguration<DocumentWorkingCopy>
{
    public void Configure(EntityTypeBuilder<DocumentWorkingCopy> builder)
    {
        builder.ToTable("DocumentWorkingCopies");
        builder.HasKey(e => e.Id);
    }
}
