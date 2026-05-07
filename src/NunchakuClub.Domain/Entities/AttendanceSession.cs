using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class AttendanceSession : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public DateTime SessionDate { get; set; }
    public string? SessionLabel { get; set; }
    public string? Notes { get; set; }

    public Guid? RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }

    public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
}
