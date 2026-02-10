using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Data.Configurations;

public class DocumentCommentConfiguration : IEntityTypeConfiguration<DocumentComment>
{
    public void Configure(EntityTypeBuilder<DocumentComment> builder)
    {
        builder.ToTable("DocumentComments");
        builder.HasKey(e => e.Id);

        // Self-referencing relationship
        builder.HasOne<DocumentComment>().WithMany().HasForeignKey(e => e.ParentCommentId).OnDelete(DeleteBehavior.Restrict);

        // Ignore computed/display properties
        builder.Ignore(e => e.CreatedByName);
        builder.Ignore(e => e.ReplyCount);
    }
}
