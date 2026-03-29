using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.CourseEnrollments.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.CourseEnrollments.Commands;

public record CreateEnrollmentCommand(CreateEnrollmentDto Dto) : IRequest<Result<Guid>>;

public class CreateEnrollmentCommandHandler : IRequestHandler<CreateEnrollmentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateEnrollmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var courseExists = await _context.Courses
            .AnyAsync(c => c.Id == dto.CourseId && c.IsActive, cancellationToken);

        if (!courseExists)
            return Result<Guid>.Failure("Course not found or inactive");

        var enrollment = new CourseEnrollment
        {
            CourseId = dto.CourseId,
            FullName = dto.FullName,
            Phone = dto.Phone,
            Email = dto.Email,
            Message = dto.Message,
            Status = EnrollmentStatus.Pending,
            EnrolledAt = DateTime.UtcNow
        };

        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(enrollment.Id);
    }
}
