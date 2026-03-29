using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.MenuItems.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.MenuItems.Commands;

public record CreateMenuItemCommand(CreateMenuItemDto Dto) : IRequest<Result<Guid>>;

public class CreateMenuItemCommandHandler : IRequestHandler<CreateMenuItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateMenuItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var menuItem = new MenuItem
        {
            Label = dto.Label,
            Url = dto.Url,
            PageId = dto.PageId,
            Target = dto.Target,
            ParentId = dto.ParentId,
            DisplayOrder = dto.DisplayOrder,
            IconClass = dto.IconClass,
            MenuLocation = dto.MenuLocation,
            IsActive = dto.IsActive
        };

        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(menuItem.Id);
    }
}
