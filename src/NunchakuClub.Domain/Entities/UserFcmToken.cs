using System;

namespace NunchakuClub.Domain.Entities;

/// <summary>
/// Lưu trữ FCM device token cho từng thiết bị / trình duyệt của một user.
/// Mỗi lần admin đăng nhập trên thiết bị mới, một bản ghi mới được thêm vào.
/// Khi push notification, hệ thống sẽ gửi đến TẤT CẢ token trong bảng này.
/// </summary>
public class UserFcmToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Foreign key đến Users.Id</summary>
    public Guid UserId { get; set; }

    /// <summary>FCM token của thiết bị / trình duyệt (≈163 ký tự, tối đa 512)</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Thời điểm token được đăng ký (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
