using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class MenuItem : BaseEntity
{
    public string Label { get; set; } = string.Empty;
    public string? Url { get; set; }
    public Guid? PageId { get; set; }
    public Page? Page { get; set; }
    public string Target { get; set; } = "_self";
    public Guid? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public int DisplayOrder { get; set; }
    public string? IconClass { get; set; }
    public string MenuLocation { get; set; } = "header";
    public bool IsActive { get; set; } = true;
    
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
}