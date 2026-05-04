using System;
using System.Collections.Generic;

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
    public List<Guid> HeadCoachIds { get; set; } = new();
    public List<Guid> AssistantCoachIds { get; set; } = new();
}
