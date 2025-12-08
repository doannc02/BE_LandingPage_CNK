using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Courses.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Courses.Queries;

public record GetCoursesQuery : IRequest<Result<List<CourseDto>>>;

public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, Result<List<CourseDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCoursesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CourseDto>>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        var courses = await _context.Courses
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                Level = c.Level.ToString(),
                DurationMonths = c.DurationMonths,
                SessionsPerWeek = c.SessionsPerWeek,
                Price = c.Price,
                IsFree = c.IsFree,
                Features = c.Features,
                ThumbnailUrl = c.ThumbnailUrl,
                IsFeatured = c.IsFeatured,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        return Result<List<CourseDto>>.Success(courses);
    }
}
