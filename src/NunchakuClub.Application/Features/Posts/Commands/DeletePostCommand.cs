using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Commands;

public record DeletePostCommand(Guid Id) : IRequest<Result<bool>>;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeletePostCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        
        if (post == null)
            return Result<bool>.Failure("Post not found");
        
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<bool>.Success(true);
    }
}
