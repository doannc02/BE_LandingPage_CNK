using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NunchakuClub.Infrastructure.Services.AI;

/// <summary>
/// Generates 768-dimensional embeddings using Google text-embedding-004
/// via direct REST call to generativelanguage.googleapis.com v1beta.
/// </summary>
public sealed class GoogleEmbeddingService : NunchakuClub.Application.Common.Interfaces.IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<GoogleEmbeddingService> _logger;

    // gemini-embedding-001 → 768 dims (matches vector(768) DB column)
    // gemini-embedding-2-preview → 3072 dims (incompatible — do NOT use as fallback)
    private const string EmbedUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent";

    public GoogleEmbeddingService(
        IOptions<GeminiSettings> opts,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleEmbeddingService> logger)
    {
        _logger = logger;
        _apiKey = opts.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("GeminiSettings:ApiKey is not configured.");

        _http = httpClientFactory.CreateClient("embedding");
    }

    public async Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
    {
        const string model = "models/gemini-embedding-001";
        var url = $"{EmbedUrl}?key={_apiKey}";

        var request = new EmbedRequest
        {
            Model = model,
            Content = new EmbedContent
            {
                Parts = new List<EmbedPart> { new() { Text = text } }
            }
        };

        using var response = await _http.PostAsJsonAsync(url, request, ct);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct);
            if (result?.Embedding?.Values is { Count: > 0 } values)
            {
                _logger.LogDebug("Embedding generated via {Model}", model);
                return values.ToArray();
            }
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(
            $"Embedding model {model} returned {(int)response.StatusCode}. Body: {errorBody}");
    }

    // ── JSON DTOs ─────────────────────────────────────────────────────────────

    private sealed class EmbedRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public EmbedContent Content { get; init; } = new();
    }

    private sealed class EmbedContent
    {
        [JsonPropertyName("parts")]
        public List<EmbedPart> Parts { get; init; } = new();
    }

    private sealed class EmbedPart
    {
        [JsonPropertyName("text")]
        public string Text { get; init; } = string.Empty;
    }

    private sealed class EmbedResponse
    {
        [JsonPropertyName("embedding")]
        public EmbedValues? Embedding { get; init; }
    }

    private sealed class EmbedValues
    {
        [JsonPropertyName("values")]
        public List<float> Values { get; init; } = new();
    }
}
