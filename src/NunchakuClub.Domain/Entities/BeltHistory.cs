using System;

namespace NunchakuClub.Domain.Entities;

public class BeltHistory : BaseEntity
{
    public Guid StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;

    // Đai cũ
    public Guid? FromBeltRankId { get; set; }
    public BeltRank? FromBeltRank { get; set; }

    // Đai mới
    public Guid ToBeltRankId { get; set; }
    public BeltRank ToBeltRank { get; set; } = null!;

    public DateTime PromotionDate { get; set; }

    public string? InstructorNote { get; set; }   // Nhận xét HLV
    public string? MediaUrl { get; set; }         // Ảnh/video buổi thi

    public Guid? RecordedByUserId { get; set; }   // ai ghi nhận
}