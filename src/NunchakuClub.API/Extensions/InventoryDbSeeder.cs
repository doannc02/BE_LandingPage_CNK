using Microsoft.EntityFrameworkCore;
using NunchakuClub.Domain.Entities;
using NunchakuClub.Infrastructure.Data.Contexts;
using System.Threading.Tasks;

namespace NunchakuClub.API.Extensions;

public static class InventoryDbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var hasCategories = await db.InventoryCategories.AnyAsync();
        if (hasCategories) return;

        var categories = new[]
        {
            new InventoryCategory { Name = "Vũ khí tập luyện", Description = "Côn nhị khúc, gậy, kiếm luyện tập" },
            new InventoryCategory { Name = "Bảo hộ", Description = "Găng tay, mũ bảo hộ, giáp ngực" },
            new InventoryCategory { Name = "Trang phục", Description = "Võ phục, đai, giày" },
            new InventoryCategory { Name = "Thiết bị lớp học", Description = "Đệm tập, bao cát, xà đơn" }
        };

        db.InventoryCategories.AddRange(categories);
        await db.SaveChangesAsync();
    }
}
