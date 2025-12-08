using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}