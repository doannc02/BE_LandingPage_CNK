using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Pages.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Pages.Commands;

public record CreatePageCommand(CreatePageDto Dto) : IRequest<Result<Guid>>;

public class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreatePageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var existingSlug = await _context.Pages.AnyAsync(p => p.Slug == dto.Slug, cancellationToken);
        if (existingSlug)
            return Result<Guid>.Failure("A page with this slug already exists");

        var page = new Page
        {
            Title = dto.Title,
            Slug = dto.Slug,
            Excerpt = dto.Excerpt,
            Content = dto.Content,
            ParentId = dto.ParentId,
            FeaturedImageUrl = dto.FeaturedImageUrl,
            BannerImageUrl = dto.BannerImageUrl,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            DisplayOrder = dto.DisplayOrder,
            IsPublished = dto.IsPublished,
            Template = dto.Template
        };

        _context.Pages.Add(page);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(page.Id);
    }
}
