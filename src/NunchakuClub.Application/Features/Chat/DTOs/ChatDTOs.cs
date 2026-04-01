using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NunchakuClub.Application.Features.Chat.Commands;

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

/// <summary>
/// Request body for POST /api/v1/chat/message (non-streaming fallback endpoint).
/// </summary>
public sealed class ChatMessageRequestDto
{
    /// <summary>Stable identifier for the user's browser session.</summary>
    [Required]
    [MinLength(1)]
    public string SessionId { get; init; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "message không được rỗng.")]
    public string Message { get; init; } = string.Empty;

    [Required]
    public List<ChatHistoryItemDto> History { get; init; } = [];
}

/// <summary>
/// Response for POST /api/v1/chat/message.
/// </summary>
public sealed class ProcessChatResponseDto
{
    /// <summary>"AI" | "HumanOnline" | "LeftMessage"</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>AI answer in Vietnamese. Non-null when Type == "AI".</summary>
    public string? Answer { get; init; }

    /// <summary>Firebase chat room ID. Non-null when Type == "HumanOnline".</summary>
    public string? ChatRoomId { get; init; }

    /// <summary>Pending message GUID. Non-null when Type == "LeftMessage".</summary>
    public string? MessageId { get; init; }

    public static ProcessChatResponseDto From(ProcessChatResult result) => new()
    {
        Type = result.ResponseType.ToString(),
        Answer = result.AiAnswer,
        ChatRoomId = result.ChatRoomId,
        MessageId = result.MessageId
    };
}
