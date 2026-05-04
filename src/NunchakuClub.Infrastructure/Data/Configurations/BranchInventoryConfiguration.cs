using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class BranchInventoryConfiguration : IEntityTypeConfiguration<BranchInventory>
{
    public void Configure(EntityTypeBuilder<BranchInventory> builder)
    {
        builder.ToTable("branch_inventories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LowStockThreshold)
            .IsRequired()
            .HasDefaultValue(5);
            
        builder.Property(x => x.ExportedThisMonth)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Ignore(x => x.Status); // Computed property

        builder.HasOne(x => x.Branch)
            .WithMany(x => x.BranchInventories)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Item)
            .WithMany(x => x.BranchInventories)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: A branch can only have one inventory record per item
        builder.HasIndex(x => new { x.BranchId, x.ItemId })
            .IsUnique();
            
        // Index for querying by branch
        builder.HasIndex(x => x.BranchId);
    }
}
