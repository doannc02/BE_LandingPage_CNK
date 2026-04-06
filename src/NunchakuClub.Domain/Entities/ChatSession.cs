using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

/// <summary>
/// Đại diện cho một phiên hội thoại giữa người dùng và hệ thống.
/// Mỗi trình duyệt có một sessionId duy nhất (lưu localStorage).
/// Một phiên có thể bắt đầu với Bot, sau đó chuyển sang Human handoff.
/// </summary>
public class ChatSession : BaseEntity
{
    /// <summary>Browser session ID (từ localStorage, key: cnk_session_id).</summary>
    public string SessionId { get; set; } = null!;

    /// <summary>User đã đăng nhập. Null = khách ẩn danh.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Trạng thái hiện tại của phiên hội thoại.</summary>
    public ChatSessionStatus Status { get; set; } = ChatSessionStatus.BotHandling;

    /// <summary>Kiểu human handoff. Null khi bot đang xử lý.</summary>
    public ChatHandoffType? HandoffType { get; set; }

    /// <summary>Firebase chat room ID khi HandoffType = Firebase.</summary>
    public string? FirebaseChatRoomId { get; set; }

    /// <summary>ID của PendingUserMessage khi HandoffType = Pending.</summary>
    public Guid? PendingMessageId { get; set; }

    /// <summary>Thời điểm phiên kết thúc.</summary>
    public DateTime? ClosedAt { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}

public enum ChatSessionStatus
{
    /// <summary>Bot đang xử lý.</summary>
    BotHandling = 0,

    /// <summary>Đã chuyển sang admin (online Firebase room hoặc offline pending).</summary>
    HumanHandoff = 1,

    /// <summary>Phiên đã kết thúc.</summary>
    Closed = 2
}

public enum ChatHandoffType
{
    /// <summary>Admin online — Firebase Realtime chat room được tạo.</summary>
    Firebase = 0,

    /// <summary>Admin offline — PendingUserMessage được tạo, FCM đã gửi.</summary>
    Pending = 1
}
