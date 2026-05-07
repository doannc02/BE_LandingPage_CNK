using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.InventoryCategories.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.InventoryCategories.Commands;

public record CreateInventoryCategoryCommand(CreateInventoryCategoryDto Dto) : IRequest<Result<Guid>>;

public class CreateInventoryCategoryCommandHandler : IRequestHandler<CreateInventoryCategoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateInventoryCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateInventoryCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = new InventoryCategory
        {
            Name = request.Dto.Name,
            Description = request.Dto.Description,
            IsActive = request.Dto.IsActive
        };

        _context.InventoryCategories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
