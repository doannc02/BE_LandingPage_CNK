using System;

namespace NunchakuClub.Domain.Entities;

public class BranchGallery : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; }

    public string MediaUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Image;

    public string? Caption { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}


public enum MediaType
{
    Image = 1,
    Video = 2
}
