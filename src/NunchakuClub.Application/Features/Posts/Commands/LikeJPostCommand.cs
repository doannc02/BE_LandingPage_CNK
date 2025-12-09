using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Commands;

public record LikePostCommand(Guid PostId) : IRequest<Result<int>>;

public class LikePostCommandHandler : IRequestHandler<LikePostCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;

    public LikePostCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<int>> Handle(LikePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts.FindAsync(new object[] { request.PostId }, cancellationToken);

        if (post == null)
            return Result<int>.Failure("Bài viết không tồn tại");

        post.LikeCount++;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(post.LikeCount);
    }
}