using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Students.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Students.Queries;

public record GetStudentByIdQuery(Guid Id) : IRequest<Result<StudentDto>>;

public class GetStudentByIdQueryHandler : IRequestHandler<GetStudentByIdQuery, Result<StudentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<StudentDto>> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        var student = await _context.StudentProfiles
            .Include(x => x.User)
            .Include(x => x.Branch)
            .Include(x => x.CurrentBeltRank)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (student == null)
            return Result<StudentDto>.Failure("Student profile not found.");

        var dto = new StudentDto
        {
            Id = student.Id,
            UserId = student.UserId,
            UserFullName = student.User.FullName,
            UserEmail = student.User.Email,
            StudentCode = student.StudentCode,
            BranchId = student.BranchId,
            BranchName = student.Branch.Name,
            CurrentBeltRankId = student.CurrentBeltRankId,
            BeltRankName = student.CurrentBeltRank?.Name,
            Address = student.Address,
            HeightCm = student.HeightCm,
            WeightKg = student.WeightKg,
            Gender = student.Gender,
            DateOfBirth = student.DateOfBirth,
            JoinDate = student.JoinDate,
            LearningStatus = student.LearningStatus,
            ClassRole = student.ClassRole,
            GuardianName = student.GuardianName,
            GuardianPhone = student.GuardianPhone,
            Notes = student.Notes
        };

        return Result<StudentDto>.Success(dto);
    }
}
