using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.AdminChat.Commands;

public record SaveFcmTokenCommand(Guid AdminId, string Token) : IRequest<Result>;

public class SaveFcmTokenCommandHandler : IRequestHandler<SaveFcmTokenCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public SaveFcmTokenCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(SaveFcmTokenCommand request, CancellationToken cancellationToken)
    {
        var token = request.Token.Trim();

        var exists = await _db.UserFcmTokens
            .AnyAsync(t => t.UserId == request.AdminId && t.Token == token, cancellationToken);

        if (!exists)
        {
            _db.UserFcmTokens.Add(new UserFcmToken
            {
                UserId = request.AdminId,
                Token = token,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
