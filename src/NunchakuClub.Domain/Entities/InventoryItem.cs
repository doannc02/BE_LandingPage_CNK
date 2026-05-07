using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    
    public Guid CategoryId { get; set; }
    public InventoryCategory Category { get; set; } = null!;
    
    public bool IsActive { get; set; } = true;
    
    public ICollection<BranchInventory> BranchInventories { get; set; } = new List<BranchInventory>();
}
