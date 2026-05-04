using System;

namespace NunchakuClub.Domain.Entities;

public class BranchStats
{
    public Guid Id { get; set; } // Map to branch.id
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Thumbnail { get; set; }
    public bool IsActive { get; set; }
    public int ActiveStudentCount { get; set; }
    public int HeadCoachCount { get; set; }
    public int AssistantCoachCount { get; set; }
}
