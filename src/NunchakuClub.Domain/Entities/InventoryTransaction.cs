using System;

namespace NunchakuClub.Domain.Entities;

public class InventoryTransaction : AuditableEntity
{
    public Guid BranchInventoryId { get; set; }
    public BranchInventory BranchInventory { get; set; } = null!;
    
    public string Type { get; set; } = "import"; // import, export
    public int Quantity { get; set; }
    public int PreviousQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string? Note { get; set; }
    
    // Thông tin thêm để truy vấn nhanh mà không cần join sâu
    public string ItemName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
}
