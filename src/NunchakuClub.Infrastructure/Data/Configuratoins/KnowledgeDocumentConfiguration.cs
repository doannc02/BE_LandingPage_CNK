using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configuratoins;

public class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Source).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Title).HasMaxLength(200);

        // 768 dimensions = Google text-embedding-004 output size
        builder.Property(x => x.Embedding).HasColumnType("vector(768)");

        // HNSW index for fast approximate nearest-neighbor search
        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);
    }
}
