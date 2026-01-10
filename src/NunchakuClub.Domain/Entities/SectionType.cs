using System;

namespace NunchakuClub.Domain.Entities;

/// <summary>
/// Định nghĩa các loại section component có thể sử dụng trong layout
/// Admin có thể enable/disable sections
/// Dev có thể thêm section types mới
/// </summary>
public class SectionType : AuditableEntity
{
    /// <summary>
    /// Tên section type (e.g., "Hero Banner", "Blog Grid", "Contact Form")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type identifier để match với frontend component
    /// (e.g., "hero", "blog-grid", "contact-form")
    /// </summary>
    public string TypeKey { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả section
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Icon class hoặc URL cho UI
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Category (e.g., "Header", "Content", "Footer")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Preview image URL
    /// </summary>
    public string? PreviewImageUrl { get; set; }

    /// <summary>
    /// JSON schema định nghĩa config fields cho section này
    /// Dùng để generate form trong admin panel
    /// Example: { "fields": [{"name": "title", "type": "text", "required": true}] }
    /// </summary>
    public string ConfigSchema { get; set; } = "{}";

    /// <summary>
    /// Default config cho section này (JSON)
    /// </summary>
    public string DefaultConfig { get; set; } = "{}";

    /// <summary>
    /// Section có active không
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Số lần section được sử dụng
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Display order trong danh sách sections
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}
