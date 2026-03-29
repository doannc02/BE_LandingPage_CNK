using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Interfaces;

/// <summary>
/// Generates dense vector embeddings from text using an external model.
/// Current implementation: Google text-embedding-004 (768 dimensions).
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Converts <paramref name="text"/> into a 768-dimensional float array.
    /// </summary>
    Task<float[]> GenerateAsync(string text, CancellationToken ct = default);
}
