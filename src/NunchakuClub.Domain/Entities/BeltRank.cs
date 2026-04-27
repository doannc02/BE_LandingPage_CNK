using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class BeltRank : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StudentProfile> StudentProfiles { get; set; } = new List<StudentProfile>();
}
