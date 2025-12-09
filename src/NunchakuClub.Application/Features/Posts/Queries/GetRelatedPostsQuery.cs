using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Posts.DTOs;
using NunchakuClub.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Queries;

public record GetRelatedPostsQuery(string Slug, int Limit = 5) : IRequest<Result<List<RelatedPostDto>>>;

public class GetRelatedPostsQueryHandler : IRequestHandler<GetRelatedPostsQuery, Result<List<RelatedPostDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetRelatedPostsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<RelatedPostDto>>> Handle(
        GetRelatedPostsQuery request,
        CancellationToken cancellationToken)
    {
        var currentPost = await _context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken);

        if (currentPost == null)
            return Result<List<RelatedPostDto>>.Failure("Bài viết không tồn tại");

        var currentTagIds = currentPost.PostTags.Select(pt => pt.TagId).ToList();

        var relatedPosts = new List<RelatedPostDto>();

        if (currentPost.CategoryId.HasValue && currentTagIds.Any())
        {
            var postsWithSameCategoryAndTags = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .Where(p => p.Id != currentPost.Id &&
                           p.Status == PostStatus.Published &&
                           p.CategoryId == currentPost.CategoryId &&
                           p.PostTags.Any(pt => currentTagIds.Contains(pt.TagId)))
                .OrderByDescending(p => p.PublishedAt)
                .Take(request.Limit)
                .Select(p => new RelatedPostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    ThumbnailUrl = p.ThumbnailUrl,
                    PublishedAt = p.PublishedAt,
                    CreatedAt = p.CreatedAt,
                    CategoryName = p.Category!.Name
                })
                .ToListAsync(cancellationToken);

            relatedPosts.AddRange(postsWithSameCategoryAndTags);
        }

        if (relatedPosts.Count < request.Limit && currentPost.CategoryId.HasValue)
        {
            var remaining = request.Limit - relatedPosts.Count;
            var postIds = relatedPosts.Select(p => p.Id).ToList();

            var postsWithSameCategory = await _context.Posts
                .Include(p => p.Category)
                .Where(p => p.Id != currentPost.Id &&
                           p.Status == PostStatus.Published &&
                           p.CategoryId == currentPost.CategoryId &&
                           !postIds.Contains(p.Id))
                .OrderByDescending(p => p.PublishedAt)
                .Take(remaining)
                .Select(p => new RelatedPostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    ThumbnailUrl = p.ThumbnailUrl,
                    PublishedAt = p.PublishedAt,
                    CreatedAt = p.CreatedAt,
                    CategoryName = p.Category!.Name
                })
                .ToListAsync(cancellationToken);

            relatedPosts.AddRange(postsWithSameCategory);
        }

        if (relatedPosts.Count < request.Limit)
        {
            var remaining = request.Limit - relatedPosts.Count;
            var postIds = relatedPosts.Select(p => p.Id).ToList();

            var latestPosts = await _context.Posts
                .Include(p => p.Category)
                .Where(p => p.Id != currentPost.Id &&
                           p.Status == PostStatus.Published &&
                           !postIds.Contains(p.Id))
                .OrderByDescending(p => p.PublishedAt)
                .Take(remaining)
                .Select(p => new RelatedPostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    ThumbnailUrl = p.ThumbnailUrl,
                    PublishedAt = p.PublishedAt,
                    CreatedAt = p.CreatedAt,
                    CategoryName = p.Category!.Name
                })
                .ToListAsync(cancellationToken);

            relatedPosts.AddRange(latestPosts);
        }

        return Result<List<RelatedPostDto>>.Success(relatedPosts);
    }
}