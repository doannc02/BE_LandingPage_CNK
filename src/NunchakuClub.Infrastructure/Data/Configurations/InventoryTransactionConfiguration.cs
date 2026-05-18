using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.ItemName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.BranchName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.HasOne(x => x.BranchInventory)
            .WithMany()
            .HasForeignKey(x => x.BranchInventoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
