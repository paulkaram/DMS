using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites");
        builder.HasKey(e => e.Id);

        // Performance indexes
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.NodeType, e.NodeId }).IsUnique();
    }
}
