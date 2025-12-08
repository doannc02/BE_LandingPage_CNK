using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Posts.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Commands;

public record CreatePostCommand(CreatePostDto Dto, Guid AuthorId) : IRequest<Result<Guid>>;

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreatePostCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        
        // Generate slug from title
        var slug = GenerateSlug(dto.Title);
        
        // Check slug uniqueness
        var existingSlug = await _context.Posts
            .AnyAsync(p => p.Slug == slug, cancellationToken);
        
        if (existingSlug)
        {
            slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
        
        var post = new Post
        {
            Title = dto.Title,
            Slug = slug,
            Content = dto.Content,
            Excerpt = dto.Excerpt,
            FeaturedImageUrl = dto.FeaturedImageUrl,
            MetaTitle = dto.MetaTitle ?? dto.Title,
            MetaDescription = dto.MetaDescription ?? dto.Excerpt,
            MetaKeywords = dto.MetaKeywords,
            CategoryId = dto.CategoryId,
            AuthorId = request.AuthorId,
            IsFeatured = dto.IsFeatured,
            Status = dto.PublishNow ? PostStatus.Published : PostStatus.Draft,
            PublishedAt = dto.PublishNow ? DateTime.UtcNow : null,
            ViewCount = 0,
            LikeCount = 0,
            CommentCount = 0
        };
        
        _context.Posts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Add tags
        if (dto.Tags.Any())
        {
            foreach (var tagName in dto.Tags)
            {
                var tagSlug = GenerateSlug(tagName);
                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Slug == tagSlug, cancellationToken);
                
                if (tag == null)
                {
                    tag = new Tag
                    {
                        Name = tagName,
                        Slug = tagSlug
                    };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                
                _context.PostTags.Add(new PostTag
                {
                    PostId = post.Id,
                    TagId = tag.Id
                });
            }
            
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        // Add images
        if (dto.ImageUrls.Any())
        {
            var displayOrder = 1;
            foreach (var imageUrl in dto.ImageUrls)
            {
                _context.PostImages.Add(new PostImage
                {
                    PostId = post.Id,
                    ImageUrl = imageUrl,
                    DisplayOrder = displayOrder++
                });
            }
            
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        return Result<Guid>.Success(post.Id);
    }
    
    private static string GenerateSlug(string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("đ", "d")
            .Replace("ă", "a")
            .Replace("â", "a")
            .Replace("ê", "e")
            .Replace("ô", "o")
            .Replace("ơ", "o")
            .Replace("ư", "u")
            .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
            .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
            .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
            .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
            .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
            .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y");
    }
}
