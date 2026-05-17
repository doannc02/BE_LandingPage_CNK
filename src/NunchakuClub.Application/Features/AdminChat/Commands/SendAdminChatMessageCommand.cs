using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.AdminChat.Commands;

public record SendAdminChatMessageCommand(
    string ChatId,
    string AdminId,
    string Text) : IRequest<Result>;

public class SendAdminChatMessageCommandHandler : IRequestHandler<SendAdminChatMessageCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IFirebaseChatService _firebaseChat;

    public SendAdminChatMessageCommandHandler(
        IApplicationDbContext db,
        IFirebaseChatService firebaseChat)
    {
        _db = db;
        _firebaseChat = firebaseChat;
    }

    public async Task<Result> Handle(SendAdminChatMessageCommand request, CancellationToken cancellationToken)
    {
        await _firebaseChat.SendMessageAsync(request.ChatId, $"admin:{request.AdminId}", request.Text, cancellationToken);

        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.FirebaseChatRoomId == request.ChatId, cancellationToken);

        if (session is not null)
        {
            _db.ConversationMessages.Add(new ConversationMessage
            {
                ChatSessionId = session.Id,
                SessionId = session.SessionId,
                Role = ConversationMessageRole.Admin,
                Content = request.Text,
                SenderAdminId = request.AdminId
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
