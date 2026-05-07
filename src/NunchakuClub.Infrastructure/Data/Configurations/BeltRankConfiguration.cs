using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class BeltRankConfiguration : IEntityTypeConfiguration<BeltRank>
{
    public void Configure(EntityTypeBuilder<BeltRank> builder)
    {
        builder.ToTable("belt_ranks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ColorHex)
            .HasMaxLength(20);

        builder.HasIndex(x => x.DisplayOrder);
    }
}
