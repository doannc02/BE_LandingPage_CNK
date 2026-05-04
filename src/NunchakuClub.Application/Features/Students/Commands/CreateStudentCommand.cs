using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Students.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Students.Commands;

public record CreateStudentCommand(CreateStudentDto Dto) : IRequest<Result<Guid>>;

public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // Check if UserId already has a StudentProfile
        var exists = await _context.StudentProfiles
            .AnyAsync(x => x.UserId == dto.UserId, cancellationToken);

        if (exists)
            return Result<Guid>.Failure("This user already has a student profile.");

        // Get User to get JoinDate (CreatedAt)
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == dto.UserId, cancellationToken);

        if (user == null)
            return Result<Guid>.Failure("User not found.");

        // check student role    
        var isStudent = await _context.Users.AnyAsync(x => x.Id == dto.UserId && x.Role == UserRole.Student, cancellationToken);
        if (!isStudent)
            return Result<Guid>.Failure("User is not a student.");

        var student = new StudentProfile
        {
            UserId = dto.UserId,
            StudentCode = dto.StudentCode.Trim(),
            BranchId = dto.BranchId,
            CurrentBeltRankId = dto.CurrentBeltRankId,
            Address = dto.Address?.Trim(),
            HeightCm = dto.HeightCm,
            WeightKg = dto.WeightKg,
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            JoinDate = user.CreatedAt, // Set JoinDate from User.CreatedAt as requested
            LearningStatus = dto.LearningStatus,
            ClassRole = dto.ClassRole,
            GuardianName = dto.GuardianName?.Trim(),
            GuardianPhone = dto.GuardianPhone?.Trim(),
            Notes = dto.Notes?.Trim()
        };

        _context.StudentProfiles.Add(student);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(student.Id);
    }
}
