namespace NunchakuClub.Domain.Entities;

public enum UserRole
{
    /// <summary>
    /// Khách — role mặc định khi đăng nhập Google lần đầu.
    /// Chỉ xem được nội dung công khai. Admin phải xác nhận mới nâng lên Student.
    /// </summary>
    Guest = 0,

    /// <summary>Quyền cao nhất — phân quyền, quản lý toàn hệ thống.</summary>
    SuperAdmin = 1,

    /// <summary>Võ sư/Quản trị hỗ trợ — quản lý Chat Rooms, học viên, RAG KB.</summary>
    SubAdmin = 2,

    /// <summary>Học viên đã được admin xác nhận — đọc/ghi thông tin cá nhân và nội dung khoá học.</summary>
    Student = 3
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}