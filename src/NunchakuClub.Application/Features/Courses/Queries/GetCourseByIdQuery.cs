using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Courses.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Courses.Queries;

public record GetCourseByIdQuery(Guid Id) : IRequest<Result<CourseDto>>;

public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, Result<CourseDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCourseByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CourseDto>> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (course == null)
            return Result<CourseDto>.Failure("Course not found");

        var dto = new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Slug = course.Slug,
            Description = course.Description,
            Level = course.Level.ToString(),
            DurationMonths = course.DurationMonths,
            SessionsPerWeek = course.SessionsPerWeek,
            Price = course.Price,
            IsFree = course.IsFree,
            Features = course.Features,
            ThumbnailUrl = course.ThumbnailUrl,
            IsFeatured = course.IsFeatured,
            IsActive = course.IsActive
        };

        return Result<CourseDto>.Success(dto);
    }
}
