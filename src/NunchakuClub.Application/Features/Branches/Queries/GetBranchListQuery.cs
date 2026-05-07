using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Branches.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Branches.Queries;

public record GetBranchListQuery(bool? IsActive = null) : IRequest<Result<List<BranchDto>>>;

public class GetBranchListQueryHandler : IRequestHandler<GetBranchListQuery, Result<List<BranchDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetBranchListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BranchDto>>> Handle(GetBranchListQuery request, CancellationToken cancellationToken)
    {
        // Use the v_branch_stats view
        var query = _context.BranchStatsView.AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var stats = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);

        // Fetch remaining standard fields from Branches if needed, or just return stats if it's enough.
        // Wait, BranchStats has Code, Name, Address, Thumbnail, IsActive.
        // We probably need to join with Branches to get all standard fields.
        var branchIds = stats.Select(x => x.Id).ToList();
        
        var branches = await _context.Branches
            .Where(x => branchIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var result = stats.Select(s => 
        {
            var b = branches[s.Id];
            return new BranchDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                ShortName = b.ShortName,
                Address = b.Address,
                Thumbnail = b.Thumbnail,
                Area = b.Area,
                Latitude = b.Latitude,
                Longitude = b.Longitude,
                Schedule = b.Schedule,
                Fee = b.Fee,
                IsFree = b.IsFree,
                Description = b.Description,
                IsActive = b.IsActive,
                ActiveStudentCount = s.ActiveStudentCount,
                HeadCoachIds = s.HeadCoachIds ?? new List<Guid>(),
                AssistantCoachIds = s.AssistantCoachIds ?? new List<Guid>()
            };
        }).ToList();

        return Result<List<BranchDto>>.Success(result);
    }
}
