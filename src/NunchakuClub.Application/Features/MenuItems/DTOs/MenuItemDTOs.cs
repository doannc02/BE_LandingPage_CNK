using System;
using System.Collections.Generic;

namespace NunchakuClub.Application.Features.MenuItems.DTOs;

public class MenuItemDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Url { get; set; }
    public Guid? PageId { get; set; }
    public string Target { get; set; } = "_self";
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public string? IconClass { get; set; }
    public string MenuLocation { get; set; } = "header";
    public bool IsActive { get; set; }
    public List<MenuItemDto> Children { get; set; } = new();
}

public class CreateMenuItemDto
{
    public string Label { get; set; } = string.Empty;
    public string? Url { get; set; }
    public Guid? PageId { get; set; }
    public string Target { get; set; } = "_self";
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public string? IconClass { get; set; }
    public string MenuLocation { get; set; } = "header";
    public bool IsActive { get; set; } = true;
}
