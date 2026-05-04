using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.InventoryCategories.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.InventoryCategories.Commands;

public record UpdateInventoryCategoryCommand(Guid Id, UpdateInventoryCategoryDto Dto) : IRequest<Result<bool>>;

public class UpdateInventoryCategoryCommandHandler : IRequestHandler<UpdateInventoryCategoryCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateInventoryCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateInventoryCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.InventoryCategories.FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
            return Result<bool>.Failure("Category not found");

        entity.Name = request.Dto.Name;
        entity.Description = request.Dto.Description;
        entity.IsActive = request.Dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
