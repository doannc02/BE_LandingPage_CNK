using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.AdminChat.Commands;

public record ReplyPendingMessageCommand(
    Guid MessageId,
    string AdminId,
    string Text) : IRequest<Result>;

public class ReplyPendingMessageCommandHandler : IRequestHandler<ReplyPendingMessageCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IFirebaseChatService _firebaseChat;

    public ReplyPendingMessageCommandHandler(
        IApplicationDbContext db,
        IFirebaseChatService firebaseChat)
    {
        _db = db;
        _firebaseChat = firebaseChat;
    }

    public async Task<Result> Handle(ReplyPendingMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _db.PendingUserMessages.FindAsync([request.MessageId], cancellationToken);
        if (message is null)
            return Result.Failure($"Không tìm thấy pending message {request.MessageId}.");

        message.Status = PendingMessageStatus.Replied;
        message.AdminReply = request.Text;
        message.AssignedAdminId = request.AdminId;
        message.RepliedAt = DateTime.UtcNow;

        await AppendAdminMessageAsync(pendingMessageId: request.MessageId, content: request.Text, adminId: request.AdminId, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        await _firebaseChat.NotifyUserReplyAsync(message.SessionId, message.AdminReply, request.AdminId, cancellationToken);

        return Result.Success();
    }

    private async Task AppendAdminMessageAsync(
        Guid pendingMessageId,
        string content,
        string adminId,
        CancellationToken ct)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.PendingMessageId == pendingMessageId, ct);

        if (session is null) return;

        _db.ConversationMessages.Add(new ConversationMessage
        {
            ChatSessionId = session.Id,
            SessionId = session.SessionId,
            Role = ConversationMessageRole.Admin,
            Content = content,
            SenderAdminId = adminId
        });
    }
}
