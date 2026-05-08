using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.InventoryCategories.DTOs;
using NunchakuClub.Application.Common.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.InventoryCategories.Queries;

public record GetInventoryCategoryListQuery(
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedList<InventoryCategoryDto>>>;

public class GetInventoryCategoryListQueryHandler : IRequestHandler<GetInventoryCategoryListQuery, Result<PaginatedList<InventoryCategoryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryCategoryListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<InventoryCategoryDto>>> Handle(GetInventoryCategoryListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InventoryCategories.AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new InventoryCategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken)
            .ContinueWith(t => Result<PaginatedList<InventoryCategoryDto>>.Success(t.Result));
    }
}
