using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class BeltHistoryConfiguration : IEntityTypeConfiguration<BeltHistory>
{
    public void Configure(EntityTypeBuilder<BeltHistory> builder)
    {
        builder.ToTable("belt_history");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.StudentProfile)
            .WithMany(x => x.BeltHistories)
            .HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FromBeltRank)
            .WithMany()
            .HasForeignKey(x => x.FromBeltRankId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ToBeltRank)
            .WithMany()
            .HasForeignKey(x => x.ToBeltRankId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
