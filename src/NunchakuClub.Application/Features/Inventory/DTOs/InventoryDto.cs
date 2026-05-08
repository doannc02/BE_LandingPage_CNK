using System;
using System.ComponentModel.DataAnnotations;

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
    
    // Flattened Item Info
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? CategoryName { get; set; }
    
    public int Quantity { get; set; }
    public int LowStockThreshold { get; set; }
    public int ExportedThisMonth { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateInventoryItemDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Sku { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public Guid BranchId { get; set; }

    [Range(0, int.MaxValue)]
    public int InitialQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; } = 5;

    public bool IsActive { get; set; } = true;
}

public class UpdateInventoryItemDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Sku { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public bool IsActive { get; set; }
}

public class AdjustStockDto
{
    [Required]
    [RegularExpression("^(import|export)$", ErrorMessage = "Type must be 'import' or 'export'")]
    public string Type { get; set; } = "import";

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
