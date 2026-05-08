using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Inventory.DTOs;
using NunchakuClub.Application.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Inventory.Queries;

public record GetInventoryListQuery(
    Guid? BranchId = null, 
    Guid? CategoryId = null, 
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedList<BranchInventoryDto>>>;

public class GetInventoryListQueryHandler : IRequestHandler<GetInventoryListQuery, Result<PaginatedList<BranchInventoryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<BranchInventoryDto>>> Handle(GetInventoryListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.BranchInventories
            .Include(x => x.Item)
                .ThenInclude(i => i.Category)
            .Include(x => x.Branch)
            .AsQueryable();

        if (request.BranchId.HasValue)
            query = query.Where(x => x.BranchId == request.BranchId.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(x => x.Item.CategoryId == request.CategoryId.Value);

        // Filter by status at the Database level
        if (!string.IsNullOrEmpty(request.Status))
        {
            if (request.Status.Equals("OutOfStock", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.Quantity <= 0);
            else if (request.Status.Equals("LowStock", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.Quantity > 0 && x.Quantity <= x.LowStockThreshold);
            else if (request.Status.Equals("InStock", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.Quantity > x.LowStockThreshold);
        }

        var paginatedResult = await query
            .OrderBy(x => x.Item.Name)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        var dtos = paginatedResult.Items.Select(x => new BranchInventoryDto
        {
            Id = x.Id,
            BranchId = x.BranchId,
            BranchName = x.Branch.Name,
            ItemId = x.ItemId,
            ItemName = x.Item.Name,
            Sku = x.Item.Sku,
            CategoryName = x.Item.Category?.Name,
            Quantity = x.Quantity,
            LowStockThreshold = x.LowStockThreshold,
            ExportedThisMonth = x.ExportedThisMonth,
            Status = x.Status.ToString()
        }).ToList();

        return Result<PaginatedList<BranchInventoryDto>>.Success(new PaginatedList<BranchInventoryDto>
        {
            Items = dtos,
            PageNumber = paginatedResult.PageNumber,
            PageSize = paginatedResult.PageSize,
            TotalCount = paginatedResult.TotalCount
        });
    }
}
