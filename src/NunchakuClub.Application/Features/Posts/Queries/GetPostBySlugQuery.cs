using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Posts.DTOs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Queries;

public record GetPostBySlugQuery(string Slug) : IRequest<Result<PostDetailDto>>;

public class GetPostBySlugQueryHandler : IRequestHandler<GetPostBySlugQuery, Result<PostDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPostBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PostDetailDto>> Handle(
        GetPostBySlugQuery request, 
        CancellationToken cancellationToken)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken);
        
        if (post == null)
            return Result<PostDetailDto>.Failure("Post not found");
        
        var dto = new PostDetailDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Content = post.Content,
            Excerpt = post.Excerpt,
            FeaturedImageUrl = post.FeaturedImageUrl,
            Status = post.Status.ToString(),
            IsFeatured = post.IsFeatured,
            PublishedAt = post.PublishedAt,
            ViewCount = post.ViewCount,
            LikeCount = post.LikeCount,
            CommentCount = post.CommentCount,
            AuthorName = post.Author.FullName,
            CategoryName = post.Category?.Name,
            CreatedAt = post.CreatedAt,
            Images = post.Images.OrderBy(i => i.DisplayOrder)
                .Select(i => new PostImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    ThumbnailUrl = i.ThumbnailUrl,
                    Caption = i.Caption,
                    AltText = i.AltText,
                    DisplayOrder = i.DisplayOrder
                }).ToList(),
            Tags = post.PostTags.Select(pt => pt.Tag.Name).ToList()
        };
        
        // Increment view count
        post.ViewCount++;
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<PostDetailDto>.Success(dto);
    }
}
