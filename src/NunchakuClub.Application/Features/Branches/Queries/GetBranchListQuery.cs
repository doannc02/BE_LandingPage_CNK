using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Branches.DTOs;
using NunchakuClub.Application.Common.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Branches.Queries;

public record GetBranchListQuery(
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedList<BranchDto>>>;

public class GetBranchListQueryHandler : IRequestHandler<GetBranchListQuery, Result<PaginatedList<BranchDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetBranchListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<BranchDto>>> Handle(GetBranchListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.BranchStatsView.AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var paginatedStats = await query
            .OrderBy(x => x.Name)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);

        var branchIds = paginatedStats.Items.Select(x => x.Id).ToList();
        
        var branches = await _context.Branches
            .Where(x => branchIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var dtos = paginatedStats.Items.Select(s => 
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

        return Result<PaginatedList<BranchDto>>.Success(new PaginatedList<BranchDto>
        {
            Items = dtos,
            PageNumber = paginatedStats.PageNumber,
            PageSize = paginatedStats.PageSize,
            TotalCount = paginatedStats.TotalCount
        });
    }
}
