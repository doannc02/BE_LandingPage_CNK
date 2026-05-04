using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.InventoryCategories.Commands;

public record DeleteInventoryCategoryCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteInventoryCategoryCommandHandler : IRequestHandler<DeleteInventoryCategoryCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteInventoryCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteInventoryCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.InventoryCategories
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
            return Result<bool>.Failure("Category not found");

        if (entity.Items.Count > 0)
            return Result<bool>.Failure("Cannot delete category with items");

        _context.InventoryCategories.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
