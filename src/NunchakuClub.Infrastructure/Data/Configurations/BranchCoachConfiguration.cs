using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class BranchCoachConfiguration : IEntityTypeConfiguration<BranchCoach>
{
    public void Configure(EntityTypeBuilder<BranchCoach> builder)
    {
        builder.ToTable("branch_coach");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(x => x.Branch)
            .WithMany(x => x.BranchCoaches)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Coach)
            .WithMany(x => x.BranchCoaches)
            .HasForeignKey(x => x.CoachId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
