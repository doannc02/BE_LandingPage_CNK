namespace NunchakuClub.Domain.Entities;

public enum UserRole
{
    Admin = 1,
    Editor = 2,
    Coach = 3,
    Member = 4
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}