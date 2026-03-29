using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Pages.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Pages.Commands;

public record UpdatePageCommand(Guid Id, UpdatePageDto Dto) : IRequest<Result<PageDto>>;

public class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, Result<PageDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdatePageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PageDto>> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _context.Pages
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (page == null)
            return Result<PageDto>.Failure("Page not found");

        var dto = request.Dto;

        var slugConflict = await _context.Pages
            .AnyAsync(p => p.Slug == dto.Slug && p.Id != request.Id, cancellationToken);
        if (slugConflict)
            return Result<PageDto>.Failure("A page with this slug already exists");

        page.Title = dto.Title;
        page.Slug = dto.Slug;
        page.Excerpt = dto.Excerpt;
        page.Content = dto.Content;
        page.ParentId = dto.ParentId;
        page.FeaturedImageUrl = dto.FeaturedImageUrl;
        page.BannerImageUrl = dto.BannerImageUrl;
        page.MetaTitle = dto.MetaTitle;
        page.MetaDescription = dto.MetaDescription;
        page.DisplayOrder = dto.DisplayOrder;
        page.IsPublished = dto.IsPublished;
        page.Template = dto.Template;
        page.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<PageDto>.Success(new PageDto
        {
            Id = page.Id,
            Title = page.Title,
            Slug = page.Slug,
            Excerpt = page.Excerpt,
            Content = page.Content,
            ParentId = page.ParentId,
            FeaturedImageUrl = page.FeaturedImageUrl,
            BannerImageUrl = page.BannerImageUrl,
            MetaTitle = page.MetaTitle,
            MetaDescription = page.MetaDescription,
            DisplayOrder = page.DisplayOrder,
            IsPublished = page.IsPublished,
            Template = page.Template
        });
    }
}
