using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class PhysicalLocationConfiguration : IEntityTypeConfiguration<PhysicalLocation>
{
    public void Configure(EntityTypeBuilder<PhysicalLocation> builder)
    {
        builder.ToTable("PhysicalLocations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.NameAr).HasMaxLength(200);
        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Path).HasMaxLength(1000);
        builder.Property(e => e.EnvironmentalConditions).HasMaxLength(500);
        builder.Property(e => e.Coordinates).HasMaxLength(100);
        builder.Property(e => e.SecurityLevel).HasMaxLength(50);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.LocationType);
    }
}

public class PhysicalItemConfiguration : IEntityTypeConfiguration<PhysicalItem>
{
    public void Configure(EntityTypeBuilder<PhysicalItem> builder)
    {
        builder.ToTable("PhysicalItems");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.Barcode).HasMaxLength(100).IsRequired();
        builder.Property(e => e.BarcodeFormat).HasMaxLength(20);
        builder.Property(e => e.Title).HasMaxLength(500).IsRequired();
        builder.Property(e => e.TitleAr).HasMaxLength(500);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Dimensions).HasMaxLength(100);
        builder.Property(e => e.ConditionNotes).HasMaxLength(1000);

        builder.HasIndex(e => e.Barcode).IsUnique();
        builder.HasIndex(e => e.LocationId);
        builder.HasIndex(e => e.DigitalDocumentId);
        builder.HasIndex(e => e.CirculationStatus);
    }
}

public class AccessionRequestConfiguration : IEntityTypeConfiguration<AccessionRequest>
{
    public void Configure(EntityTypeBuilder<AccessionRequest> builder)
    {
        builder.ToTable("AccessionRequests");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.AccessionNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReviewNotes).HasMaxLength(1000);
        builder.Property(e => e.AcceptanceNotes).HasMaxLength(1000);
        builder.Property(e => e.RejectionReason).HasMaxLength(1000);

        builder.HasIndex(e => e.AccessionNumber).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}

public class AccessionRequestItemConfiguration : IEntityTypeConfiguration<AccessionRequestItem>
{
    public void Configure(EntityTypeBuilder<AccessionRequestItem> builder)
    {
        builder.ToTable("AccessionRequestItems");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.Title).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => e.AccessionRequestId);
    }
}

public class CirculationRecordConfiguration : IEntityTypeConfiguration<CirculationRecord>
{
    public void Configure(EntityTypeBuilder<CirculationRecord> builder)
    {
        builder.ToTable("CirculationRecords");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.Purpose).HasMaxLength(500);
        builder.Property(e => e.ConditionNotes).HasMaxLength(1000);

        builder.HasIndex(e => e.PhysicalItemId);
        builder.HasIndex(e => e.BorrowerId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.DueDate);
    }
}

public class CustodyTransferConfiguration : IEntityTypeConfiguration<CustodyTransfer>
{
    public void Configure(EntityTypeBuilder<CustodyTransfer> builder)
    {
        builder.ToTable("CustodyTransfers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        builder.Property(e => e.ReferenceType).HasMaxLength(100);
        builder.Property(e => e.EntryHash).HasMaxLength(128);
        builder.Property(e => e.PreviousEntryHash).HasMaxLength(128);

        builder.HasIndex(e => e.PhysicalItemId);
        builder.HasIndex(e => e.TransferredAt);
    }
}
