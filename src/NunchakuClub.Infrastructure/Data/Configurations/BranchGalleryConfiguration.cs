using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class BranchGalleryConfiguration : IEntityTypeConfiguration<BranchGallery>
{
    public void Configure(EntityTypeBuilder<BranchGallery> builder)
    {
        builder.ToTable("branch_gallery");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MediaType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(x => x.Branch)
            .WithMany(x => x.BranchGalleries)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
