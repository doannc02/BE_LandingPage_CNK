using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.AdminChat.DTOs;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.AdminChat.Queries;

public record GetPendingCountQuery : IRequest<Result<PendingCountDto>>;

public class GetPendingCountQueryHandler : IRequestHandler<GetPendingCountQuery, Result<PendingCountDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPendingCountQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PendingCountDto>> Handle(
        GetPendingCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await _db.PendingUserMessages
            .CountAsync(m => m.Status == PendingMessageStatus.Pending
                          || m.Status == PendingMessageStatus.Assigned,
                cancellationToken);

        return Result<PendingCountDto>.Success(new PendingCountDto(count));
    }
}
