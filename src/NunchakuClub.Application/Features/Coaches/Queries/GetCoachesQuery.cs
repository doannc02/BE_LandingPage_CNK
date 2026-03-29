using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Coaches.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Coaches.Queries;

public record GetCoachesQuery(bool? IsActive = null) : IRequest<Result<List<CoachDto>>>;

public class GetCoachesQueryHandler : IRequestHandler<GetCoachesQuery, Result<List<CoachDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCoachesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CoachDto>>> Handle(GetCoachesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Coaches.AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        var coaches = await query
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CoachDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Title = c.Title,
                Bio = c.Bio,
                Specialization = c.Specialization,
                YearsOfExperience = c.YearsOfExperience,
                Certifications = c.Certifications,
                Achievements = c.Achievements,
                AvatarUrl = c.AvatarUrl,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        return Result<List<CoachDto>>.Success(coaches);
    }
}
