using System;

namespace NunchakuClub.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public Guid AttendanceSessionId { get; set; }
    public AttendanceSession AttendanceSession { get; set; } = null!;

    public Guid StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public string? Note { get; set; }
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    ExcusedAbsent = 4
}
