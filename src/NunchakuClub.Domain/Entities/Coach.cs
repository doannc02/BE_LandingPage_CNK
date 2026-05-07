using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Coach : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public int YearsOfExperience { get; set; }
    public string[]? Certifications { get; set; }
    public string[]? Achievements { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<Achievement> CoachAchievements { get; set; } = new List<Achievement>();
}