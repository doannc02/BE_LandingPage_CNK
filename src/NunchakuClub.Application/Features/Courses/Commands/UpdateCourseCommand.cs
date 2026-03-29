using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Courses.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Courses.Commands;

public record UpdateCourseCommand(Guid Id, UpdateCourseDto Dto) : IRequest<Result<CourseDto>>;

public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, Result<CourseDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CourseDto>> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (course == null)
            return Result<CourseDto>.Failure("Course not found");

        var dto = request.Dto;
        course.Name = dto.Name;
        course.Description = dto.Description;
        course.Level = Enum.Parse<CourseLevel>(dto.Level);
        course.DurationMonths = dto.DurationMonths;
        course.SessionsPerWeek = dto.SessionsPerWeek;
        course.Price = dto.Price;
        course.IsFree = dto.IsFree;
        course.Features = dto.Features?.ToArray();
        course.ThumbnailUrl = dto.ThumbnailUrl;
        course.IsFeatured = dto.IsFeatured;
        course.IsActive = dto.IsActive;
        course.DisplayOrder = dto.DisplayOrder;
        course.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CourseDto>.Success(new CourseDto
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
        });
    }
}
