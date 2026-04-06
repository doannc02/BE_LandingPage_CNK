using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Chat.Queries;

public record GetChatHistoryQuery(string SessionId) : IRequest<Result<ChatSessionDto>>;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record ChatSessionDto(
    string SessionId,
    string Status,
    string? HandoffType,
    string? FirebaseChatRoomId,
    string? PendingMessageId,
    IReadOnlyList<ConversationMessageDto> Messages
);

public sealed record ConversationMessageDto(
    Guid Id,
    string Role,
    string Content,
    string? SenderAdminId,
    DateTime CreatedAt
);

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetChatHistoryQueryHandler
    : IRequestHandler<GetChatHistoryQuery, Result<ChatSessionDto>>
{
    private readonly IApplicationDbContext _db;

    public GetChatHistoryQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ChatSessionDto>> Handle(
        GetChatHistoryQuery request, CancellationToken ct)
    {
        // Lấy session đang active (hoặc session Closed gần nhất nếu không có active)
        var session = await _db.ChatSessions
            .Where(s => s.SessionId == request.SessionId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (session is null)
            return Result<ChatSessionDto>.Success(new ChatSessionDto(
                request.SessionId, "None", null, null, null,
                Array.Empty<ConversationMessageDto>()));

        var messages = await _db.ConversationMessages
            .Where(m => m.ChatSessionId == session.Id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ConversationMessageDto(
                m.Id,
                m.Role.ToString(),
                m.Content,
                m.SenderAdminId,
                m.CreatedAt))
            .ToListAsync(ct);

        return Result<ChatSessionDto>.Success(new ChatSessionDto(
            session.SessionId,
            session.Status.ToString(),
            session.HandoffType?.ToString(),
            session.FirebaseChatRoomId,
            session.PendingMessageId?.ToString(),
            messages));
    }
}
