using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Commands;

public record PublishPostCommand(Guid Id) : IRequest<Result<bool>>;

public class PublishPostCommandHandler : IRequestHandler<PublishPostCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public PublishPostCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(PublishPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        
        if (post == null)
            return Result<bool>.Failure("Post not found");
        
        post.Status = PostStatus.Published;
        post.PublishedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<bool>.Success(true);
    }
}
