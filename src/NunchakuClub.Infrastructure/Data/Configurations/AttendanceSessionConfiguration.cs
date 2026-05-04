using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ToTable("attendance_sessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionLabel)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Branch)
            .WithMany(x => x.AttendanceSessions)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RecordedByUser)
            .WithMany(x => x.RecordedAttendanceSessions)
            .HasForeignKey(x => x.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.BranchId, x.SessionDate });
    }
}
