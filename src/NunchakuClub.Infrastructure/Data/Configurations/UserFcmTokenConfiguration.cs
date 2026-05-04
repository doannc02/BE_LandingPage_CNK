using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class UserFcmTokenConfiguration : IEntityTypeConfiguration<UserFcmToken>
{
    public void Configure(EntityTypeBuilder<UserFcmToken> builder)
    {
        builder.ToTable("user_fcm_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now() AT TIME ZONE 'utc'");

        // Đảm bảo không lưu trùng token cho cùng một user
        builder.HasIndex(t => new { t.UserId, t.Token })
            .IsUnique();

        builder.HasOne(t => t.User)
            .WithMany(u => u.FcmTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
