using System;
using System.Collections.Generic;

namespace NunchakuClub.Application.Features.Layouts.DTOs;

public class SectionTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TypeKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public string? PreviewImageUrl { get; set; }
    public SectionConfigSchemaDto ConfigSchema { get; set; } = new();
    public Dictionary<string, object> DefaultConfig { get; set; } = new();
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Schema định nghĩa config fields cho section type
/// Dùng để generate form trong admin panel
/// </summary>
public class SectionConfigSchemaDto
{
    public List<ConfigFieldDto> Fields { get; set; } = new();
}

public class ConfigFieldDto
{
    /// <summary>
    /// Field name (key in config object)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Field label hiển thị trong form
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Field type (text, textarea, number, color, image, select, etc.)
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// Field có required không
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// Default value
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Placeholder text
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Help text
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Options cho select/radio (nếu có)
    /// </summary>
    public List<SelectOptionDto>? Options { get; set; }

    /// <summary>
    /// Validation rules
    /// </summary>
    public Dictionary<string, object>? Validation { get; set; }
}

public class SelectOptionDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class CreateSectionTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string TypeKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public string? PreviewImageUrl { get; set; }
    public SectionConfigSchemaDto ConfigSchema { get; set; } = new();
    public Dictionary<string, object> DefaultConfig { get; set; } = new();
    public int DisplayOrder { get; set; } = 0;
}
