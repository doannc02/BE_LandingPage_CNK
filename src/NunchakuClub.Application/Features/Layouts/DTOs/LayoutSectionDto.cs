using System;
using System.Collections.Generic;

namespace NunchakuClub.Application.Features.Layouts.DTOs;

/// <summary>
/// DTO cho một section trong layout
/// </summary>
public class LayoutSectionDto
{
    /// <summary>
    /// Unique ID của section trong layout (client-generated)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type của section (map tới SectionType.TypeKey)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Config data cho section này (dynamic object)
    /// </summary>
    public Dictionary<string, object> Config { get; set; } = new();

    /// <summary>
    /// Display order trong page
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Section có visible không (admin có thể hide tạm)
    /// </summary>
    public bool IsVisible { get; set; } = true;
}
