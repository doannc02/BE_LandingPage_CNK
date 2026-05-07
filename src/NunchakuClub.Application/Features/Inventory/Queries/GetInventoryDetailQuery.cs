using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Inventory.DTOs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Inventory.Queries;

public record GetInventoryDetailQuery(Guid Id) : IRequest<Result<BranchInventoryDto>>;

public class GetInventoryDetailQueryHandler : IRequestHandler<GetInventoryDetailQuery, Result<BranchInventoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BranchInventoryDto>> Handle(GetInventoryDetailQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.BranchInventories
            .Include(x => x.Item)
                .ThenInclude(i => i.Category)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
            return Result<BranchInventoryDto>.Failure("Inventory not found");

        var dto = new BranchInventoryDto
        {
            Id = entity.Id,
            BranchId = entity.BranchId,
            BranchName = entity.Branch.Name,
            Quantity = entity.Quantity,
            LowStockThreshold = entity.LowStockThreshold,
            ExportedThisMonth = entity.ExportedThisMonth,
            Status = entity.Status.ToString(),
            Item = new InventoryItemDto
            {
                Id = entity.Item.Id,
                Name = entity.Item.Name,
                Sku = entity.Item.Sku,
                Description = entity.Item.Description,
                ImageUrl = entity.Item.ImageUrl,
                CategoryId = entity.Item.CategoryId,
                CategoryName = entity.Item.Category?.Name ?? string.Empty,
                IsActive = entity.Item.IsActive
            }
        };

        return Result<BranchInventoryDto>.Success(dto);
    }
}
