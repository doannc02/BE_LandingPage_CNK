using System;
using System.Collections.Generic;

namespace NunchakuClub.Application.Features.Courses.DTOs;

public class CourseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public int SessionsPerWeek { get; set; }
    public decimal Price { get; set; }
    public bool IsFree { get; set; }
    public string[]? Features { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCourseDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Level { get; set; } = "Beginner";
    public int DurationMonths { get; set; }
    public int SessionsPerWeek { get; set; }
    public decimal Price { get; set; }
    public bool IsFree { get; set; }
    public List<string> Features { get; set; } = new();
    public string? ThumbnailUrl { get; set; }
    public bool IsFeatured { get; set; }
}
