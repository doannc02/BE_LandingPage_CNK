using System;
using System.Collections.Generic;

namespace NunchakuClub.Application.Features.Layouts.DTOs;

public class LayoutTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int UsageCount { get; set; }
    public PageLayoutDto Layout { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLayoutTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public string? Category { get; set; }
    public bool IsDefault { get; set; } = false;
    public PageLayoutDto Layout { get; set; } = new();
}

public class UpdateLayoutTemplateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
    public PageLayoutDto? Layout { get; set; }
}
