using System;

namespace NunchakuClub.Application.Features.AdminChat.DTOs;

public sealed record PendingMessageDto(
    Guid Id,
    string SessionId,
    string UserMessage,
    string Status,
    string? AdminReply,
    string? AssignedAdminId,
    DateTime CreatedAt,
    DateTime? RepliedAt
);

public sealed record PendingCountDto(int Count);
