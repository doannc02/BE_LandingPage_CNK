using System;

namespace NunchakuClub.Application.Features.Achievements.DTOs;

public class AchievementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime AchievementDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? CoachId { get; set; }
    public string? CoachName { get; set; }
    public string? ParticipantNames { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateAchievementDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime AchievementDate { get; set; }
    public string Type { get; set; } = "Competition";
    public string? ImageUrl { get; set; }
    public Guid? CoachId { get; set; }
    public string? ParticipantNames { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
}
