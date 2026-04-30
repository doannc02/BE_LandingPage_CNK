using System;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Coaches.DTOs;

public class CoachDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public CoachTitle Title { get; set; }
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public int YearsOfExperience { get; set; }
    public string[]? Certifications { get; set; }
    public string[]? Achievements { get; set; }
    public string? AvatarUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCoachDto
{
    public string FullName { get; set; } = string.Empty;
    public CoachTitle Title { get; set; } = CoachTitle.AssistantCoach;
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public int YearsOfExperience { get; set; }
    public string[]? Certifications { get; set; }
    public string[]? Achievements { get; set; }
    public string? AvatarUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCoachDto
{
    public string FullName { get; set; } = string.Empty;
    public CoachTitle Title { get; set; } = CoachTitle.AssistantCoach;
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public int YearsOfExperience { get; set; }
    public string[]? Certifications { get; set; }
    public string[]? Achievements { get; set; }
    public string? AvatarUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
