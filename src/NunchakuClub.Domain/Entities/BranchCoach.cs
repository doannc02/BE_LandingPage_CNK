using System;

namespace NunchakuClub.Domain.Entities;

public class BranchCoach : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public Guid CoachId { get; set; }
    public Coach Coach { get; set; } = null!;

    public CoachTitle Title { get; set; } = CoachTitle.AssistantCoach;
}