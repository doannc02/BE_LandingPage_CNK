using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.CourseEnrollments.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.CourseEnrollments.Queries;

public record GetEnrollmentsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<Result<List<CourseEnrollmentDto>>>;

public class GetEnrollmentsQueryHandler : IRequestHandler<GetEnrollmentsQuery, Result<List<CourseEnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEnrollmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CourseEnrollmentDto>>> Handle(GetEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        var enrollments = await _context.CourseEnrollments
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrolledAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new CourseEnrollmentDto
            {
                Id = e.Id,
                CourseId = e.CourseId,
                CourseName = e.Course.Name,
                FullName = e.FullName,
                Phone = e.Phone,
                Email = e.Email,
                Message = e.Message,
                Status = e.Status.ToString(),
                EnrolledAt = e.EnrolledAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<CourseEnrollmentDto>>.Success(enrollments);
    }
}
