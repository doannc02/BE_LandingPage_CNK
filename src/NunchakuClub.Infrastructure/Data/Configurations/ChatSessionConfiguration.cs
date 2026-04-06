using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SessionId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.HandoffType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.FirebaseChatRoomId)
            .HasMaxLength(128);

        // Tìm session đang active theo sessionId
        builder.HasIndex(s => s.SessionId);

        // Tìm session theo Firebase chat room ID (admin close chat)
        builder.HasIndex(s => s.FirebaseChatRoomId)
            .HasFilter("firebase_chat_room_id IS NOT NULL");

        // Tìm session theo PendingMessageId (admin reply pending)
        builder.HasIndex(s => s.PendingMessageId)
            .HasFilter("pending_message_id IS NOT NULL");

        builder.HasMany(s => s.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
