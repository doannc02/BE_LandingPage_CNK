using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Pages.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Pages.Queries;

public record GetPagesQuery(bool? IsPublished = null) : IRequest<Result<List<PageDto>>>;

public class GetPagesQueryHandler : IRequestHandler<GetPagesQuery, Result<List<PageDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<PageDto>>> Handle(GetPagesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Pages.AsQueryable();

        if (request.IsPublished.HasValue)
            query = query.Where(p => p.IsPublished == request.IsPublished.Value);

        var pages = await query
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new PageDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Excerpt = p.Excerpt,
                Content = p.Content,
                ParentId = p.ParentId,
                FeaturedImageUrl = p.FeaturedImageUrl,
                BannerImageUrl = p.BannerImageUrl,
                MetaTitle = p.MetaTitle,
                MetaDescription = p.MetaDescription,
                DisplayOrder = p.DisplayOrder,
                IsPublished = p.IsPublished,
                Template = p.Template
            })
            .ToListAsync(cancellationToken);

        return Result<List<PageDto>>.Success(pages);
    }
}
