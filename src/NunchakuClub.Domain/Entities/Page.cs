using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Page : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public Guid? ParentId { get; set; }
    public Page? Parent { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool ShowInMenu { get; set; } = true;
    public string? Template { get; set; }

    // === Custom Layout System ===

    /// <summary>
    /// Layout template được sử dụng cho page này
    /// Nếu null, page sẽ dùng LayoutConfig riêng
    /// </summary>
    public Guid? LayoutTemplateId { get; set; }
    public LayoutTemplate? LayoutTemplate { get; set; }

    /// <summary>
    /// Custom layout config cho page này (JSON)
    /// Override hoặc customize từ template
    /// Format: { "sections": [...], "theme": {...} }
    /// </summary>
    public string? LayoutConfig { get; set; }

    /// <summary>
    /// Version của layout (để rollback)
    /// </summary>
    public int LayoutVersion { get; set; } = 1;

    public ICollection<Page> Children { get; set; } = new List<Page>();
}