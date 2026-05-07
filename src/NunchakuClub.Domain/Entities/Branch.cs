using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Branch : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? Address { get; set; }
    public string? Thumbnail { get; set; }
    public string? Area { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Schedule { get; set; }
    public string? Fee { get; set; }
    public bool IsFree { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StudentProfile> StudentProfiles { get; set; } = new List<StudentProfile>();
    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    public ICollection<BranchGallery> BranchGalleries { get; set; } = new List<BranchGallery>();
    public ICollection<BranchCoach> BranchCoaches { get; set; } = new List<BranchCoach>();
}
