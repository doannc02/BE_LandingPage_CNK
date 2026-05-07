using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.InventoryCategories.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.InventoryCategories.Queries;

public record GetInventoryCategoryListQuery(bool? IsActive = null) : IRequest<Result<List<InventoryCategoryDto>>>;

public class GetInventoryCategoryListQueryHandler : IRequestHandler<GetInventoryCategoryListQuery, Result<List<InventoryCategoryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryCategoryListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<InventoryCategoryDto>>> Handle(GetInventoryCategoryListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InventoryCategories.AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var categories = await query
            .OrderBy(x => x.Name)
            .Select(x => new InventoryCategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Result<List<InventoryCategoryDto>>.Success(categories);
    }
}
