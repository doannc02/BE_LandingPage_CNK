using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Interfaces;

/// <summary>
/// Manages the knowledge base used for RAG retrieval.
/// SearchAsync uses pgvector cosine-similarity; document management methods
/// generate embeddings automatically via IEmbeddingService.
/// </summary>
public interface IKnowledgeBaseService
{
    // ── Retrieval ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the top-k most semantically relevant knowledge chunks for the query.
    /// Falls back to keyword search when the DB has no embedded documents.
    /// </summary>
    Task<IReadOnlyList<KnowledgeChunk>> SearchAsync(
        string query,
        int topK = 2,
        CancellationToken ct = default);

    // ── Document management ──────────────────────────────────────────────────

    Task<IReadOnlyList<KnowledgeDocumentDto>> GetAllAsync(CancellationToken ct = default);

    Task<KnowledgeDocumentDto> AddAsync(
        string content, string source, string? title,
        CancellationToken ct = default);

    Task<KnowledgeDocumentDto> UpdateAsync(
        Guid id, string content, string source, string? title,
        CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Seeds the default CLB knowledge documents if the table is empty.</summary>
    Task SeedDefaultAsync(CancellationToken ct = default);
}

/// <param name="Content">Raw text chunk sent to Gemini as context.</param>
/// <param name="Source">Identifier of the source (e.g. "hoc-phi").</param>
/// <param name="Score">Cosine similarity score (0–1). 1 = perfect match.</param>
public record KnowledgeChunk(string Content, string Source, float Score);

public sealed class KnowledgeDocumentDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Title { get; init; }
    public bool HasEmbedding { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
