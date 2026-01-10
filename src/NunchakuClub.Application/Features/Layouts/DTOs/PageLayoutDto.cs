using System;
using System.Collections.Generic;

namespace NunchakuClub.Application.Features.Layouts.DTOs;

/// <summary>
/// DTO cho toàn bộ layout configuration của một page
/// </summary>
public class PageLayoutDto
{
    /// <summary>
    /// Layout template ID (nếu dùng template)
    /// </summary>
    public Guid? LayoutTemplateId { get; set; }

    /// <summary>
    /// Tên template (nếu có)
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Sections trong layout
    /// </summary>
    public List<LayoutSectionDto> Sections { get; set; } = new();

    /// <summary>
    /// Theme override cho page này (optional)
    /// </summary>
    public ThemeConfigDto? Theme { get; set; }

    /// <summary>
    /// Custom CSS (optional)
    /// </summary>
    public string? CustomCss { get; set; }

    /// <summary>
    /// Custom JS (optional)
    /// </summary>
    public string? CustomJs { get; set; }

    /// <summary>
    /// Version của layout
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// DTO cho theme configuration
/// </summary>
public class ThemeConfigDto
{
    /// <summary>
    /// Color palette
    /// </summary>
    public ColorPaletteDto? Colors { get; set; }

    /// <summary>
    /// Typography settings
    /// </summary>
    public TypographyDto? Typography { get; set; }

    /// <summary>
    /// Logo URL
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Favicon URL
    /// </summary>
    public string? FaviconUrl { get; set; }

    /// <summary>
    /// Custom properties (key-value pairs)
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

public class ColorPaletteDto
{
    public string? Primary { get; set; }
    public string? Secondary { get; set; }
    public string? Accent { get; set; }
    public string? Background { get; set; }
    public string? Text { get; set; }
    public string? TextSecondary { get; set; }
    public string? Border { get; set; }
    public string? Success { get; set; }
    public string? Warning { get; set; }
    public string? Error { get; set; }
}

public class TypographyDto
{
    public string? FontFamily { get; set; }
    public string? HeadingFont { get; set; }
    public string? BaseFontSize { get; set; }
    public Dictionary<string, string>? FontSizes { get; set; }
    public Dictionary<string, string>? FontWeights { get; set; }
}
