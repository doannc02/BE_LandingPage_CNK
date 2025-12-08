using System;

namespace NunchakuClub.Domain.Entities;

public class Achievement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime AchievementDate { get; set; }
    public AchievementType Type { get; set; } = AchievementType.Competition;
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public Guid? CoachId { get; set; }
    public Coach? Coach { get; set; }
    public string? ParticipantNames { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
}

public enum AchievementType
{
    Competition = 1,
    Certification = 2,
    Milestone = 3,
    Award = 4
}