using System;
using System.Collections.Generic;

namespace NunchakuClub.Application.Features.Branches.DTOs;

public class BranchDto
{
    public Guid Id { get; set; }
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
    public bool IsActive { get; set; }
    
    // Stats
    public int ActiveStudentCount { get; set; }
    public List<Guid> HeadCoachIds { get; set; } = new();
    public List<Guid> AssistantCoachIds { get; set; } = new();
}

public class BranchDetailDto : BranchDto
{
    public List<BranchGalleryDto> Galleries { get; set; } = new();
    public List<BranchCoachDto> Coaches { get; set; } = new();
    public List<BranchStudentLeaderDto> StudentLeaders { get; set; } = new();
}

public class BranchGalleryDto
{
    public Guid Id { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
}

public class BranchCoachDto
{
    public Guid CoachId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Title { get; set; } = string.Empty; // HeadCoach or AssistantCoach
}

public class BranchStudentLeaderDto
{
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Monitor or ViceMonitor
}

public class CreateBranchDto
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
}

public class UpdateBranchDto : CreateBranchDto
{
}
