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
        var trimmedStudentCode = dto.StudentCode.Trim();

        var existingProfile = await _context.StudentProfiles
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(x => x.UserId == dto.UserId, cancellationToken);

        if (existingProfile != null)
        {
            if (existingProfile.IsDeleted)
            {
                return Result<Guid>.Failure(
                    "This student profile is deleted. Please contact admin to restore it.");
            }

            return Result<Guid>.Failure(
                "This user already has a student profile.");
        }


        var deletedCodeExists = await _context.StudentProfiles
            .IgnoreQueryFilters()
            .AnyAsync(x => x.StudentCode == trimmedStudentCode && x.IsDeleted, cancellationToken);

        if (deletedCodeExists)
            return Result<Guid>.Failure($"This student profile with code {trimmedStudentCode} is deleted. Please contact admin to restore it.");

        var codeExists = await _context.StudentProfiles
            .AnyAsync(x => x.StudentCode == trimmedStudentCode, cancellationToken);

        if (codeExists)
            return Result<Guid>.Failure("Student code already exists.");

        // Get User to get JoinDate (CreatedAt)
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == dto.UserId, cancellationToken);

        if (user == null)
            return Result<Guid>.Failure("User not found.");

        if (user.Role != UserRole.Student)
            return Result<Guid>.Failure("User is not a student.");

        var student = new StudentProfile
        {
            UserId = dto.UserId,
            StudentCode = trimmedStudentCode,
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
