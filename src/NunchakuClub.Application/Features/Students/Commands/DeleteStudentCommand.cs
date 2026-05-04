using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Students.Commands;

public record DeleteStudentCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteStudentCommandHandler : IRequestHandler<DeleteStudentCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.StudentProfiles
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (student == null)
            return Result<bool>.Failure("Student profile not found.");

        _context.StudentProfiles.Remove(student);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
