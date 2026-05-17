using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class CoachConfiguration : IEntityTypeConfiguration<Coach>
{
    public void Configure(EntityTypeBuilder<Coach> builder)
    {
        builder.ToTable("coaches");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Title)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(c => c.Bio)
            .HasMaxLength(2000);

        builder.Property(c => c.Specialization)
            .HasMaxLength(500);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(255);

        builder.Property(c => c.AvatarUrl)
            .HasMaxLength(1000);

        builder.Property(c => c.CoverImageUrl)
            .HasMaxLength(1000);

        builder.Property(c => c.Certifications)
            .HasColumnType("jsonb");

        builder.Property(c => c.Achievements)
            .HasColumnType("jsonb");

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.IsActive);
    }
}
