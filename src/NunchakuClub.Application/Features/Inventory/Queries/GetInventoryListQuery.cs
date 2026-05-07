using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Inventory.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Inventory.Queries;

public record GetInventoryListQuery(Guid? BranchId = null, Guid? CategoryId = null, string? Status = null) : IRequest<Result<List<BranchInventoryDto>>>;

public class GetInventoryListQueryHandler : IRequestHandler<GetInventoryListQuery, Result<List<BranchInventoryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BranchInventoryDto>>> Handle(GetInventoryListQuery request, CancellationToken cancellationToken)
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

        // Fetch to memory if status filtering is required (since Status is computed)
        var inventories = await query.ToListAsync(cancellationToken);

        if (!string.IsNullOrEmpty(request.Status))
        {
            inventories = inventories.Where(x => x.Status.ToString().Equals(request.Status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var result = inventories.Select(x => new BranchInventoryDto
        {
            Id = x.Id,
            BranchId = x.BranchId,
            BranchName = x.Branch.Name,
            Quantity = x.Quantity,
            LowStockThreshold = x.LowStockThreshold,
            ExportedThisMonth = x.ExportedThisMonth,
            Status = x.Status.ToString(),
            Item = new InventoryItemDto
            {
                Id = x.Item.Id,
                Name = x.Item.Name,
                Sku = x.Item.Sku,
                Description = x.Item.Description,
                ImageUrl = x.Item.ImageUrl,
                CategoryId = x.Item.CategoryId,
                CategoryName = x.Item.Category?.Name ?? string.Empty,
                IsActive = x.Item.IsActive
            }
        }).ToList();

        return Result<List<BranchInventoryDto>>.Success(result);
    }
}
