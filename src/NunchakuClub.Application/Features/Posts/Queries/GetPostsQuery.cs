using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Extensions;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Posts.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Queries;

public record GetPostsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    Guid? CategoryId = null,
    PostStatus? Status = null,
    bool? IsFeatured = null
) : IRequest<Result<PaginatedList<PostDto>>>;

public class GetPostsQueryHandler : IRequestHandler<GetPostsQuery, Result<PaginatedList<PostDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPostsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<PostDto>>> Handle(
        GetPostsQuery request, 
        CancellationToken cancellationToken)
    {
        var query = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .AsQueryable();
        
        // Filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p => 
                p.Title.Contains(request.SearchTerm) || 
                p.Content.Contains(request.SearchTerm));
        }
        
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }
        
        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }
        
        if (request.IsFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == request.IsFeatured.Value);
        }
        
        // Order by
        query = query.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);
        
        // Map to DTO
        var postsQuery = query.Select(p => new PostDto
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            Excerpt = p.Excerpt,
            FeaturedImageUrl = p.FeaturedImageUrl,
            Status = p.Status.ToString(),
            IsFeatured = p.IsFeatured,
            PublishedAt = p.PublishedAt,
            ViewCount = p.ViewCount,
            LikeCount = p.LikeCount,
            CommentCount = p.CommentCount,
            AuthorName = p.Author.FullName,
            CategoryName = p.Category != null ? p.Category.Name : null,
            CreatedAt = p.CreatedAt
        });
        
        var paginatedList = await postsQuery.ToPaginatedListAsync(
            request.PageNumber, 
            request.PageSize, 
            cancellationToken);
        
        return Result<PaginatedList<PostDto>>.Success(paginatedList);
    }
}
