using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Inventory.DTOs;
using NunchakuClub.Domain.Entities;
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
            .Include(x => x.Item)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (branchInventory == null)
            return Result<bool>.Failure("Inventory not found");

        int previousQuantity = branchInventory.Quantity;
        int newQuantity = previousQuantity;

        if (request.Dto.Type.Equals("export", StringComparison.OrdinalIgnoreCase))
        {
            if (branchInventory.Quantity < request.Dto.Quantity)
                return Result<bool>.Failure($"Không đủ hàng trong kho. Tồn kho hiện tại: {branchInventory.Quantity}, yêu cầu: {request.Dto.Quantity}");

            branchInventory.Quantity -= request.Dto.Quantity;
            branchInventory.ExportedThisMonth += request.Dto.Quantity;
            newQuantity = branchInventory.Quantity;
        }
        else if (request.Dto.Type.Equals("import", StringComparison.OrdinalIgnoreCase))
        {
            branchInventory.Quantity += request.Dto.Quantity;
            newQuantity = branchInventory.Quantity;
        }
        else
        {
            return Result<bool>.Failure("Loại điều chỉnh không hợp lệ. Vui lòng dùng 'import' hoặc 'export'");
        }

        // Record Transaction
        var transaction = new InventoryTransaction
        {
            BranchInventoryId = branchInventory.Id,
            Type = request.Dto.Type.ToLower(),
            Quantity = request.Dto.Quantity,
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
            Note = request.Dto.Note,
            ItemName = branchInventory.Item.Name,
            BranchName = branchInventory.Branch.Name
        };

        _context.InventoryTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
