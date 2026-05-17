using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Label)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Url)
            .HasMaxLength(1000);

        builder.Property(m => m.Target)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.IconClass)
            .HasMaxLength(100);

        builder.Property(m => m.MenuLocation)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Page)
            .WithMany()
            .HasForeignKey(m => m.PageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.MenuLocation);
    }
}
