using System;

namespace NunchakuClub.Domain.Entities;

public class PendingUserMessage : BaseEntity
{
    public string SessionId { get; set; } = null!;
    public string UserMessage { get; set; } = null!;
    public string? UserId { get; set; }
    public PendingMessageStatus Status { get; set; } = PendingMessageStatus.Pending;
    public string? AdminReply { get; set; }
    public string? AssignedAdminId { get; set; }
    public DateTime? RepliedAt { get; set; }
    public int NotificationRetryCount { get; set; }
    public DateTime? NextNotificationAt { get; set; }
}

public enum PendingMessageStatus
{
    Pending = 0,
    Assigned = 1,
    Replied = 2,
    Closed = 3
}
