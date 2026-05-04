using System;

namespace NunchakuClub.Domain.Entities;

public class BranchInventory : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    
    public Guid ItemId { get; set; }
    public InventoryItem Item { get; set; } = null!;
    
    public int Quantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public int ExportedThisMonth { get; set; }
    
    public StockStatus Status
    {
        get
        {
            if (Quantity <= 0) return StockStatus.OutOfStock;
            if (Quantity <= LowStockThreshold) return StockStatus.LowStock;
            return StockStatus.InStock;
        }
    }
}

public enum StockStatus 
{ 
    InStock = 1, 
    LowStock = 2, 
    OutOfStock = 3 
}
