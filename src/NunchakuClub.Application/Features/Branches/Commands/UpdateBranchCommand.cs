using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Branches.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Branches.Commands;

public record UpdateBranchCommand(Guid Id, UpdateBranchDto Dto) : IRequest<Result<bool>>;

public class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateBranchCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FindAsync(new object[] { request.Id }, cancellationToken);

        if (branch == null)
            return Result<bool>.Failure("Branch not found");

        branch.Code = request.Dto.Code;
        branch.Name = request.Dto.Name;
        branch.ShortName = request.Dto.ShortName;
        branch.Address = request.Dto.Address;
        branch.Thumbnail = request.Dto.Thumbnail;
        branch.Area = request.Dto.Area;
        branch.Latitude = request.Dto.Latitude;
        branch.Longitude = request.Dto.Longitude;
        branch.Schedule = request.Dto.Schedule;
        branch.Fee = request.Dto.Fee;
        branch.IsFree = request.Dto.IsFree;
        branch.Description = request.Dto.Description;
        branch.IsActive = request.Dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
