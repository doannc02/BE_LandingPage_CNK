using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("conversation_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.SessionId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(m => m.SenderAdminId)
            .HasMaxLength(128);

        // Lấy toàn bộ history của một session (dùng ở GetChatHistoryQuery)
        builder.HasIndex(m => new { m.SessionId, m.CreatedAt });
    }
}
