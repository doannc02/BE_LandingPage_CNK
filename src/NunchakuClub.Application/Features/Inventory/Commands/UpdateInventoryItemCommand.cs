using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Inventory.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Inventory.Commands;

public record UpdateInventoryItemCommand(Guid Id, UpdateInventoryItemDto Dto) : IRequest<Result<bool>>;

public class UpdateInventoryItemCommandHandler : IRequestHandler<UpdateInventoryItemCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateInventoryItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var branchInventory = await _context.BranchInventories
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (branchInventory == null)
            return Result<bool>.Failure("Inventory not found");

        var item = branchInventory.Item;
        item.Name = request.Dto.Name;
        item.Sku = request.Dto.Sku;
        item.Description = request.Dto.Description;
        item.ImageUrl = request.Dto.ImageUrl;
        item.CategoryId = request.Dto.CategoryId;
        item.IsActive = request.Dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
