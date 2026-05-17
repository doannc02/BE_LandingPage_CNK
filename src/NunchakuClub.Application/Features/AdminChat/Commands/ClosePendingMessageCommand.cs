using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.AdminChat.Commands;

public record ClosePendingMessageCommand(Guid MessageId) : IRequest<Result>;

public class ClosePendingMessageCommandHandler : IRequestHandler<ClosePendingMessageCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public ClosePendingMessageCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(ClosePendingMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _db.PendingUserMessages.FindAsync([request.MessageId], cancellationToken);
        if (message is null)
            return Result.Failure($"Không tìm thấy pending message {request.MessageId}.");

        message.Status = PendingMessageStatus.Closed;

        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(
                s => s.PendingMessageId == request.MessageId && s.Status != ChatSessionStatus.Closed,
                cancellationToken);

        if (session is not null)
        {
            session.Status = ChatSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
