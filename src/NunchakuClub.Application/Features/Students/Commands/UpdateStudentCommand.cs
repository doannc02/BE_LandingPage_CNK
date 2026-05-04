using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Students.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Students.Commands;

public record UpdateStudentCommand(Guid Id, UpdateStudentDto Dto) : IRequest<Result<bool>>;

public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.StudentProfiles
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (student == null)
            return Result<bool>.Failure("Student profile not found.");

        var dto = request.Dto;

        student.StudentCode = dto.StudentCode.Trim();
        student.BranchId = dto.BranchId;
        student.CurrentBeltRankId = dto.CurrentBeltRankId;
        student.Address = dto.Address?.Trim();
        student.HeightCm = dto.HeightCm;
        student.WeightKg = dto.WeightKg;
        student.Gender = dto.Gender;
        student.DateOfBirth = dto.DateOfBirth;
        student.LearningStatus = dto.LearningStatus;
        student.ClassRole = dto.ClassRole;
        student.GuardianName = dto.GuardianName?.Trim();
        student.GuardianPhone = dto.GuardianPhone?.Trim();
        student.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
