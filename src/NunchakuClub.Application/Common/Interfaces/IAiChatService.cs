using System.Collections.Generic;
using System.Threading;
using NunchakuClub.Application.Features.Chat.DTOs;

namespace NunchakuClub.Application.Common.Interfaces;

/// <summary>
/// Streams AI-generated responses token-by-token via IAsyncEnumerable.
/// </summary>
public interface IAiChatService
{
    IAsyncEnumerable<string> StreamAsync(
        ChatStreamRequest request,
        CancellationToken ct = default);
}
