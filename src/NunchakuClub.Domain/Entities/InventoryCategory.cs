using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class InventoryCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
}
