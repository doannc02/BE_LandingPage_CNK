using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class StudentProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string StudentCode { get; set; } = string.Empty;

    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public Guid? CurrentBeltRankId { get; set; }
    public BeltRank? CurrentBeltRank { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;

    public StudentLearningStatus LearningStatus { get; set; } = StudentLearningStatus.Active;
    public StudentClassRole ClassRole { get; set; } = StudentClassRole.Member;

    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? Notes { get; set; }

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}

public enum StudentLearningStatus
{
    Active = 1,
    Paused = 2,
    Left = 3
}

public enum StudentClassRole
{
    Member = 1,
    ViceMonitor = 2,
    Monitor = 3
}
