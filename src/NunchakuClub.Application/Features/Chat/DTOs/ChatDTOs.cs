using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NunchakuClub.Application.Features.Chat.DTOs;

/// <summary>
/// Represents a single turn in the conversation history (user or assistant).
/// Maps 1-to-1 with the Frontend TypeScript type ChatHistoryItem.
/// </summary>
public record ChatHistoryItem(
    [property: Required] string Role,    // "user" | "assistant"
    [property: Required] string Content
);

/// <summary>
/// Internal domain request passed to IAiChatService.
/// </summary>
public record ChatStreamRequest(
    string Message,
    IReadOnlyList<ChatHistoryItem> History
);

/// <summary>
/// JSON body received from the Next.js proxy (matches Frontend ChatRequest type).
/// </summary>
public sealed class ChatRequestDto
{
    [Required]
    [MinLength(1, ErrorMessage = "message không được rỗng.")]
    public string Message { get; init; } = string.Empty;

    [Required]
    public List<ChatHistoryItemDto> History { get; init; } = [];
}

public sealed class ChatHistoryItemDto
{
    [Required]
    public string Role { get; init; } = string.Empty;   // "user" | "assistant"

    public string Content { get; init; } = string.Empty;
}
