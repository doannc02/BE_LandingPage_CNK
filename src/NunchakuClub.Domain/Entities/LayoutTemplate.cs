using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

/// <summary>
/// Layout template có thể tái sử dụng cho nhiều pages
/// Admin tạo template với sections được cấu hình sẵn
/// </summary>
public class LayoutTemplate : AuditableEntity
{
    /// <summary>
    /// Tên template (e.g., "Homepage Hero + Blog", "Landing Page", "About Us")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Slug để reference template
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả template
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Preview thumbnail URL
    /// </summary>
    public string? PreviewImageUrl { get; set; }

    /// <summary>
    /// Cấu hình layout dưới dạng JSON
    /// Chứa array of sections với config
    /// </summary>
    public string LayoutConfig { get; set; } = "{}";

    /// <summary>
    /// Category của template (e.g., "Homepage", "Landing", "Content Page")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Template có active không (admin có thể disable)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Template mặc định cho pages mới
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Số lần template được sử dụng
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Pages sử dụng template này
    /// </summary>
    public ICollection<Page> Pages { get; set; } = new List<Page>();
}
