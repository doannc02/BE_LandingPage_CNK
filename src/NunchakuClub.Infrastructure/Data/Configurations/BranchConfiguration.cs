using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ShortName)
            .HasMaxLength(200);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Area)
            .HasMaxLength(100);

        builder.Property(x => x.Latitude)
            .HasColumnType("numeric(10,7)");

        builder.Property(x => x.Longitude)
            .HasColumnType("numeric(10,7)");

        builder.Property(x => x.Schedule)
            .HasMaxLength(100);

        builder.Property(x => x.Fee)
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);
    }
}
