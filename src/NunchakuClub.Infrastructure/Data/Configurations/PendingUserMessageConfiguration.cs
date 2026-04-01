using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class PendingUserMessageConfiguration : IEntityTypeConfiguration<PendingUserMessage>
{
    public void Configure(EntityTypeBuilder<PendingUserMessage> builder)
    {
        builder.ToTable("pending_user_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.UserMessage)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.UserId)
            .HasMaxLength(128);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(PendingMessageStatus.Pending);

        builder.Property(x => x.AdminReply)
            .HasMaxLength(4000);

        builder.Property(x => x.AssignedAdminId)
            .HasMaxLength(128);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.NextNotificationAt);
    }
}
