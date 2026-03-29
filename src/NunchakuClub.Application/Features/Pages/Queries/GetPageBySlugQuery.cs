using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Pages.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Pages.Queries;

public record GetPageBySlugQuery(string Slug) : IRequest<Result<PageDto>>;

public class GetPageBySlugQueryHandler : IRequestHandler<GetPageBySlugQuery, Result<PageDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPageBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PageDto>> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var page = await _context.Pages
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken);

        if (page == null)
            return Result<PageDto>.Failure("Page not found");

        var dto = new PageDto
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
        };

        return Result<PageDto>.Success(dto);
    }
}
