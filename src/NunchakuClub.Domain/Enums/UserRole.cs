namespace NunchakuClub.Domain.Entities;

public enum UserRole
{
    /// <summary>Quyền cao nhất — phân quyền, quản lý toàn hệ thống.</summary>
    SuperAdmin = 1,

    /// <summary>Võ sư/Quản trị hỗ trợ — quản lý Chat Rooms, học viên, RAG KB.</summary>
    SubAdmin = 2,

    /// <summary>Học viên / Khách — chỉ đọc/ghi thông tin cá nhân.</summary>
    Student = 3
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}