using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Inventory.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Inventory.Commands;

public record CreateInventoryItemCommand(CreateInventoryItemDto Dto) : IRequest<Result<Guid>>;

public class CreateInventoryItemCommandHandler : IRequestHandler<CreateInventoryItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateInventoryItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = new InventoryItem
        {
            Name = request.Dto.Name,
            Sku = request.Dto.Sku,
            Description = request.Dto.Description,
            ImageUrl = request.Dto.ImageUrl,
            CategoryId = request.Dto.CategoryId,
            IsActive = request.Dto.IsActive
        };

        _context.InventoryItems.Add(item);

        var branchInventory = new BranchInventory
        {
            BranchId = request.Dto.BranchId,
            Item = item,
            Quantity = request.Dto.InitialQuantity,
            LowStockThreshold = request.Dto.LowStockThreshold,
            ExportedThisMonth = 0
        };

        _context.BranchInventories.Add(branchInventory);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(branchInventory.Id);
    }
}
