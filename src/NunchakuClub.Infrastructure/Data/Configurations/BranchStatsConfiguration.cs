using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class BranchStatsConfiguration : IEntityTypeConfiguration<BranchStats>
{
    public void Configure(EntityTypeBuilder<BranchStats> builder)
    {
        builder.HasNoKey();
        builder.ToView("v_branch_stats");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Code).HasColumnName("code");
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.Address).HasColumnName("address");
        builder.Property(x => x.Thumbnail).HasColumnName("thumbnail");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.ActiveStudentCount).HasColumnName("active_student_count");
        builder.Property(x => x.HeadCoachCount).HasColumnName("head_coach_count");
        builder.Property(x => x.AssistantCoachCount).HasColumnName("assistant_coach_count");
    }
}
