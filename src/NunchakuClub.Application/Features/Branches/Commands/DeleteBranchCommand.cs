using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Branches.Commands;

public record DeleteBranchCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteBranchCommandHandler : IRequestHandler<DeleteBranchCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteBranchCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FindAsync(new object[] { request.Id }, cancellationToken);

        if (branch == null)
            return Result<bool>.Failure("Branch not found");

        // Ideally we should check if there are any related active students or inventory before deleting
        // Or soft delete it
        
        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
