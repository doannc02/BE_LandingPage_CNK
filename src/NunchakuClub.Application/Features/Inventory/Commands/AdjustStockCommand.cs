using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Inventory.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Inventory.Commands;

public record AdjustStockCommand(Guid Id, AdjustStockDto Dto) : IRequest<Result<bool>>;

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public AdjustStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var branchInventory = await _context.BranchInventories
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (branchInventory == null)
            return Result<bool>.Failure("Inventory not found");

        if (request.Dto.Quantity <= 0)
            return Result<bool>.Failure("Quantity must be greater than zero");

        if (request.Dto.Type.Equals("export", StringComparison.OrdinalIgnoreCase))
        {
            if (branchInventory.Quantity < request.Dto.Quantity)
                return Result<bool>.Failure($"Not enough stock. Current stock: {branchInventory.Quantity}");

            branchInventory.Quantity -= request.Dto.Quantity;
            branchInventory.ExportedThisMonth += request.Dto.Quantity;
        }
        else if (request.Dto.Type.Equals("import", StringComparison.OrdinalIgnoreCase))
        {
            branchInventory.Quantity += request.Dto.Quantity;
        }
        else
        {
            return Result<bool>.Failure("Invalid adjustment type. Use 'import' or 'export'");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
