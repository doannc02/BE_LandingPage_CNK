using System;

namespace NunchakuClub.Domain.Entities;

/// <summary>
/// Một tin nhắn trong phiên hội thoại — có thể là user, bot, hoặc admin.
/// </summary>
public class ConversationMessage : BaseEntity
{
    /// <summary>FK → ChatSession.Id</summary>
    public Guid ChatSessionId { get; set; }

    /// <summary>
    /// Browser session ID (denormalized) để query không cần join.
    /// Luôn bằng ChatSession.SessionId.
    /// </summary>
    public string SessionId { get; set; } = null!;

    /// <summary>Ai gửi tin nhắn này.</summary>
    public ConversationMessageRole Role { get; set; }

    /// <summary>Nội dung tin nhắn.</summary>
    public string Content { get; set; } = null!;

    /// <summary>Admin ID nếu Role == Admin (null với User và Bot).</summary>
    public string? SenderAdminId { get; set; }

    public ChatSession Session { get; set; } = null!;
}

public enum ConversationMessageRole
{
    User = 0,
    Bot = 1,
    Admin = 2
}
