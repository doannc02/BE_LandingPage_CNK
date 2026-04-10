using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Chat.Queries;

public record GetChatNotificationQuery(string SessionId) : IRequest<Result<ChatNotificationDto?>>;

public sealed record ChatNotificationDto(
    string PendingId,
    string Reply,
    DateTime RepliedAt,
    string Status
);

public sealed class GetChatNotificationQueryHandler
    : IRequestHandler<GetChatNotificationQuery, Result<ChatNotificationDto?>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public GetChatNotificationQueryHandler(IApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<ChatNotificationDto?>> Handle(
        GetChatNotificationQuery request, CancellationToken ct)
    {
        var cacheKey = $"notification:{request.SessionId}";

        var cached = await _cache.GetAsync<ChatNotificationDto>(cacheKey);
        if (cached is not null)
            return Result<ChatNotificationDto?>.Success(cached);

        var pending = await _db.PendingUserMessages
            .Where(p => p.SessionId == request.SessionId
                     && (p.Status == PendingMessageStatus.Replied || p.Status == PendingMessageStatus.Closed)
                     && p.AdminReply != null
                     && p.RepliedAt != null)
            .OrderByDescending(p => p.RepliedAt)
            .FirstOrDefaultAsync(ct);

        if (pending is null)
            return Result<ChatNotificationDto?>.Success(null);

        var dto = new ChatNotificationDto(
            PendingId: pending.Id.ToString(),
            Reply: pending.AdminReply!,
            RepliedAt: pending.RepliedAt!.Value,
            Status: pending.Status.ToString()
        );

        await _cache.SetAsync(cacheKey, dto, CacheTtl);

        return Result<ChatNotificationDto?>.Success(dto);
    }
}
