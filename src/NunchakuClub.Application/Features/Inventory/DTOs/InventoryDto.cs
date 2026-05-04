using System;

namespace NunchakuClub.Application.Features.Inventory.DTOs;

public class InventoryItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class BranchInventoryDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public InventoryItemDto Item { get; set; } = null!;
    public int Quantity { get; set; }
    public int LowStockThreshold { get; set; }
    public int ExportedThisMonth { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateInventoryItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public Guid BranchId { get; set; } // The branch this item is initially created for
    public int InitialQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
}

public class UpdateInventoryItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsActive { get; set; }
}

public class AdjustStockDto
{
    public string Type { get; set; } = "import"; // "import" or "export"
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
