using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Posts.DTOs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Commands;

public record UpdatePostCommand(Guid Id, UpdatePostDto Dto) : IRequest<Result<bool>>;

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdatePostCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts
            .Include(p => p.PostTags)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        
        if (post == null)
            return Result<bool>.Failure("Post not found");
        
        var dto = request.Dto;
        
        post.Title = dto.Title;
        post.Content = dto.Content;
        post.Excerpt = dto.Excerpt;
        post.FeaturedImageUrl = dto.FeaturedImageUrl;
        post.MetaTitle = dto.MetaTitle ?? dto.Title;
        post.MetaDescription = dto.MetaDescription ?? dto.Excerpt;
        post.MetaKeywords = dto.MetaKeywords;
        post.CategoryId = dto.CategoryId;
        post.IsFeatured = dto.IsFeatured;
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<bool>.Success(true);
    }
}
