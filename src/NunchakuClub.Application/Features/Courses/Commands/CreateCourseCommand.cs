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

public record CreateCourseCommand(CreateCourseDto Dto) : IRequest<Result<Guid>>;

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var slug = dto.Name.ToLowerInvariant().Replace(" ", "-");
        
        var course = new Course
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            Level = Enum.Parse<CourseLevel>(dto.Level),
            DurationMonths = dto.DurationMonths,
            SessionsPerWeek = dto.SessionsPerWeek,
            Price = dto.Price,
            IsFree = dto.IsFree,
            Features = dto.Features.ToArray(),
            ThumbnailUrl = dto.ThumbnailUrl,
            IsFeatured = dto.IsFeatured,
            IsActive = true,
            DisplayOrder = 0
        };
        
        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(course.Id);
    }
}
