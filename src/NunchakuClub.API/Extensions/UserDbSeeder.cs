using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Domain.Entities;
using NunchakuClub.Infrastructure.Data.Contexts;
using System.Threading.Tasks;

namespace NunchakuClub.API.Extensions;

public static class UserDbSeeder
{
    public static async Task SeedAdminAsync(ApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        var hasAdmin = await db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin);
        if (hasAdmin) return;

        var admin = new User
        {
            Email = "admin@nunchakuclub.vn",
            Username = "admin",
            FullName = "Super Admin",
            PasswordHash = passwordHasher.HashPassword("Admin@123456"),
            Role = UserRole.SuperAdmin,
            Status = UserStatus.Active,
            EmailVerified = true
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
