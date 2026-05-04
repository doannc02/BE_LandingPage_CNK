using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Inventory.Commands;

public record DeleteInventoryItemCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteInventoryItemCommandHandler : IRequestHandler<DeleteInventoryItemCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteInventoryItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var branchInventory = await _context.BranchInventories
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (branchInventory == null)
            return Result<bool>.Failure("Inventory not found");

        _context.BranchInventories.Remove(branchInventory);
        
        // Notice: This removes the branch inventory record. The underlying item remains in `InventoryItems` table 
        // to keep historical records if it's used elsewhere. If we want to hard delete the item, we need to check if 
        // it's used by other branches first.

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
