using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.AdminChat.Commands;

public record CloseChatCommand(string ChatId) : IRequest<Result>;

public class CloseChatCommandHandler : IRequestHandler<CloseChatCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IFirebaseChatService _firebaseChat;

    public CloseChatCommandHandler(
        IApplicationDbContext db,
        IFirebaseChatService firebaseChat)
    {
        _db = db;
        _firebaseChat = firebaseChat;
    }

    public async Task<Result> Handle(CloseChatCommand request, CancellationToken cancellationToken)
    {
        await _firebaseChat.CloseChatAsync(request.ChatId, cancellationToken);

        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(
                s => s.FirebaseChatRoomId == request.ChatId && s.Status != ChatSessionStatus.Closed,
                cancellationToken);

        if (session is not null)
        {
            session.Status = ChatSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
