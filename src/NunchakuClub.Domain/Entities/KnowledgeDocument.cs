using System;
using Pgvector;

namespace NunchakuClub.Domain.Entities;

/// <summary>
/// Stores knowledge base documents with pre-computed embeddings for RAG retrieval.
/// Embedding is a 768-dimensional vector from Google text-embedding-004.
/// </summary>
public class KnowledgeDocument : BaseEntity
{
    /// <summary>Plain-text content chunk sent to Gemini as context.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Human-readable source tag (e.g. "hoc-phi", "lich-hoc").</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Optional display title for admin UI.</summary>
    public string? Title { get; set; }

    /// <summary>768-dimensional embedding vector (Google text-embedding-004).</summary>
    public Vector? Embedding { get; set; }
}
