using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.MenuItems.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.MenuItems.Queries;

public record GetMenuItemsQuery(string? Location = null) : IRequest<Result<List<MenuItemDto>>>;

public class GetMenuItemsQueryHandler : IRequestHandler<GetMenuItemsQuery, Result<List<MenuItemDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetMenuItemsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<MenuItemDto>>> Handle(GetMenuItemsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MenuItems
            .Where(m => m.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Location))
            query = query.Where(m => m.MenuLocation == request.Location);

        var allItems = await query
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(cancellationToken);

        var rootItems = allItems
            .Where(m => m.ParentId == null)
            .Select(m => MapToDto(m, allItems))
            .ToList();

        return Result<List<MenuItemDto>>.Success(rootItems);
    }

    private static MenuItemDto MapToDto(Domain.Entities.MenuItem item, List<Domain.Entities.MenuItem> allItems)
    {
        var dto = new MenuItemDto
        {
            Id = item.Id,
            Label = item.Label,
            Url = item.Url,
            PageId = item.PageId,
            Target = item.Target,
            ParentId = item.ParentId,
            DisplayOrder = item.DisplayOrder,
            IconClass = item.IconClass,
            MenuLocation = item.MenuLocation,
            IsActive = item.IsActive,
            Children = allItems
                .Where(c => c.ParentId == item.Id)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => MapToDto(c, allItems))
                .ToList()
        };
        return dto;
    }
}
