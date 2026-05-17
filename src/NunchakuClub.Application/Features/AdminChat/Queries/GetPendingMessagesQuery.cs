using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.AdminChat.DTOs;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.AdminChat.Queries;

public record GetPendingMessagesQuery(
    PendingMessageStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<List<PendingMessageDto>>>;

public class GetPendingMessagesQueryHandler
    : IRequestHandler<GetPendingMessagesQuery, Result<List<PendingMessageDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetPendingMessagesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<PendingMessageDto>>> Handle(
        GetPendingMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(1, request.Page);

        var query = _db.PendingUserMessages.AsQueryable();

        query = request.Status.HasValue
            ? query.Where(m => m.Status == request.Status.Value)
            : query.Where(m => m.Status == PendingMessageStatus.Pending
                             || m.Status == PendingMessageStatus.Assigned);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new PendingMessageDto(
                m.Id,
                m.SessionId,
                m.UserMessage,
                m.Status.ToString(),
                m.AdminReply,
                m.AssignedAdminId,
                m.CreatedAt,
                m.RepliedAt))
            .ToListAsync(cancellationToken);

        return Result<List<PendingMessageDto>>.Success(items);
    }
}
