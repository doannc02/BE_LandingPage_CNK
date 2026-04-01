using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NunchakuClub.Application.Features.Chat.DTOs;

namespace NunchakuClub.Application.Common.Interfaces;

public interface IFallbackClassifierService
{
    /// <summary>
    /// Single Gemini call: retrieves KB context, generates answer, and scores confidence.
    /// Returns structured decision to determine AI vs. human routing.
    /// </summary>
    Task<FallbackDecision> ClassifyAsync(
        string userMessage,
        IReadOnlyList<ChatHistoryItem> history,
        CancellationToken ct = default);
}

/// <summary>
/// Result of the fallback classification step.
/// </summary>
public sealed record FallbackDecision(
    /// <summary>0.0 – 1.0. Routing threshold: >= 0.75 → AI answers directly.</summary>
    float Confidence,
    /// <summary>Force human regardless of confidence (complaint, payment dispute, etc.).</summary>
    bool NeedsHuman,
    /// <summary>address | fee | schedule | registration | online | training | discipline | legal | complaint | other</summary>
    string Category,
    /// <summary>AI-generated answer in Vietnamese, ready to display to user.</summary>
    string Answer
);
