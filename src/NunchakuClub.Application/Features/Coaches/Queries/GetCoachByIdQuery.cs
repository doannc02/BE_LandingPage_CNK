using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Coaches.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Coaches.Queries;

public record GetCoachByIdQuery(Guid Id) : IRequest<Result<CoachDto>>;

public class GetCoachByIdQueryHandler : IRequestHandler<GetCoachByIdQuery, Result<CoachDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCoachByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CoachDto>> Handle(GetCoachByIdQuery request, CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (coach == null)
            return Result<CoachDto>.Failure("Coach not found");

        var dto = new CoachDto
        {
            Id = coach.Id,
            FullName = coach.FullName,
            Title = coach.Title,
            Bio = coach.Bio,
            Specialization = coach.Specialization,
            YearsOfExperience = coach.YearsOfExperience,
            Certifications = coach.Certifications,
            Achievements = coach.Achievements,
            AvatarUrl = coach.AvatarUrl,
            DisplayOrder = coach.DisplayOrder,
            IsActive = coach.IsActive
        };

        return Result<CoachDto>.Success(dto);
    }
}
