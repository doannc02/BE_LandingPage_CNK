using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Branches.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Branches.Commands;

public record CreateBranchCommand(CreateBranchDto Dto) : IRequest<Result<Guid>>;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateBranchCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = new Branch
        {
            Code = request.Dto.Code,
            Name = request.Dto.Name,
            ShortName = request.Dto.ShortName,
            Address = request.Dto.Address,
            Thumbnail = request.Dto.Thumbnail,
            Area = request.Dto.Area,
            Latitude = request.Dto.Latitude,
            Longitude = request.Dto.Longitude,
            Schedule = request.Dto.Schedule,
            Fee = request.Dto.Fee,
            IsFree = request.Dto.IsFree,
            Description = request.Dto.Description,
            IsActive = request.Dto.IsActive
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(branch.Id);
    }
}
