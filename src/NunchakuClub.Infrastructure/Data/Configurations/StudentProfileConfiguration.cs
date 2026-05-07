using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("student_profiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StudentCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.StudentCode)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.LearningStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ClassRole)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.GuardianName)
            .HasMaxLength(255);

        builder.Property(x => x.GuardianPhone)
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.User)
            .WithOne(x => x.StudentProfile)
            .HasForeignKey<StudentProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Branch)
            .WithMany(x => x.StudentProfiles)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CurrentBeltRank)
            .WithMany(x => x.StudentProfiles)
            .HasForeignKey(x => x.CurrentBeltRankId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.BranchId, x.LearningStatus });
    }
}
