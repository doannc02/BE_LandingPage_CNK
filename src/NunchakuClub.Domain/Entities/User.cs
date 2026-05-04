using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Guest;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool EmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }


    /// <summary>
    /// Firebase UID — được set khi người dùng đăng nhập qua SSO (Google/Email Firebase).
    /// Dùng để liên kết account nội bộ với Firebase Auth.
    /// </summary>
    public string? FirebaseUid { get; set; }
    
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public StudentProfile? StudentProfile { get; set; }
    public ICollection<AttendanceSession> RecordedAttendanceSessions { get; set; } = new List<AttendanceSession>();
}

    /// <summary>
    /// Tất cả FCM device tokens của user này (nhiều thiết bị / trình duyệt).
    /// </summary>
    public ICollection<UserFcmToken> FcmTokens { get; set; } = new List<UserFcmToken>();
}
