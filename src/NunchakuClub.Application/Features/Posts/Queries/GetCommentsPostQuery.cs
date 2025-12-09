using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Posts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Queries;

public record GetCommentsQuery(Guid PostId) : IRequest<Result<List<CommentDto>>>;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, Result<List<CommentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCommentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CommentDto>>> Handle(
        GetCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == request.PostId &&
                       c.ParentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                AuthorName = c.AuthorName,
                CreatedAt = c.CreatedAt,
                ParentId = c.ParentId,
                Replies = c.Replies
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new CommentDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        AuthorName = r.AuthorName,
                        CreatedAt = r.CreatedAt,
                        ParentId = r.ParentId
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Result<List<CommentDto>>.Success(comments);
    }
}