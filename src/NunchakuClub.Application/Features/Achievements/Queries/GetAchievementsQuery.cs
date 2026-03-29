using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Achievements.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Achievements.Queries;

public record GetAchievementsQuery(bool? IsFeatured = null, Guid? CoachId = null) : IRequest<Result<List<AchievementDto>>>;

public class GetAchievementsQueryHandler : IRequestHandler<GetAchievementsQuery, Result<List<AchievementDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAchievementsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AchievementDto>>> Handle(GetAchievementsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Achievements
            .Include(a => a.Coach)
            .AsQueryable();

        if (request.IsFeatured.HasValue)
            query = query.Where(a => a.IsFeatured == request.IsFeatured.Value);

        if (request.CoachId.HasValue)
            query = query.Where(a => a.CoachId == request.CoachId.Value);

        var achievements = await query
            .OrderBy(a => a.DisplayOrder)
            .ThenByDescending(a => a.AchievementDate)
            .Select(a => new AchievementDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                AchievementDate = a.AchievementDate,
                Type = a.Type.ToString(),
                ImageUrl = a.ImageUrl,
                CoachId = a.CoachId,
                CoachName = a.Coach != null ? a.Coach.FullName : null,
                ParticipantNames = a.ParticipantNames,
                IsFeatured = a.IsFeatured,
                DisplayOrder = a.DisplayOrder
            })
            .ToListAsync(cancellationToken);

        return Result<List<AchievementDto>>.Success(achievements);
    }
}
