using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ToTable("media_files");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Filename)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.OriginalFilename)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(m => m.FileUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(m => m.ThumbnailUrl)
            .HasMaxLength(1000);

        builder.Property(m => m.FileType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.MimeType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Title)
            .HasMaxLength(500);

        builder.Property(m => m.AltText)
            .HasMaxLength(500);

        builder.Property(m => m.Caption)
            .HasMaxLength(1000);

        builder.Property(m => m.Description)
            .HasMaxLength(2000);

        builder.HasOne(m => m.Uploader)
            .WithMany()
            .HasForeignKey(m => m.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.UploadedBy);
    }
}
