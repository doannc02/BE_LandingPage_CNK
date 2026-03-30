namespace NunchakuClub.Infrastructure.Services.AI;

/// <summary>
/// Bound from appsettings section "GeminiSettings".
/// </summary>
public sealed class GeminiSettings
{
    /// <summary>Google AI Studio API key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gemini model identifier, e.g. "gemini-1.5-flash".</summary>
    public string ModelId { get; set; } = "gemini-1.5-flash";

    /// <summary>
    /// Maximum number of history turns sent to the model (oldest are dropped first).
    /// Each "turn" = one user message + one assistant reply.
    /// </summary>
    public int MaxHistoryTurns { get; set; } = 10;

    /// <summary>
    /// Optional static API key for server-to-server authentication with Next.js proxy.
    /// If empty, the Authorization header is not validated.
    /// </summary>
    public string BackendApiKey { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } 
    public string ApiVersion { get; set; } 
}
