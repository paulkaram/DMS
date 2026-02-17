using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DocumentShareConfiguration : IEntityTypeConfiguration<DocumentShare>
{
    public void Configure(EntityTypeBuilder<DocumentShare> builder)
    {
        builder.ToTable("DocumentShares");
        builder.HasKey(e => e.Id);

        // Ignore computed/display properties
        builder.Ignore(e => e.DocumentName);
        builder.Ignore(e => e.SharedWithUserName);
        builder.Ignore(e => e.SharedByUserName);

        // OTP field constraints
        builder.Property(e => e.OtpCode).HasMaxLength(6);

        // Link sharing
        builder.Property(e => e.ShareToken).HasMaxLength(64);
        builder.HasIndex(e => e.ShareToken)
            .IsUnique()
            .HasFilter("[ShareToken] IS NOT NULL");

        // Performance indexes
        builder.HasIndex(e => e.DocumentId);
        builder.HasIndex(e => e.SharedWithUserId);
    }
}
