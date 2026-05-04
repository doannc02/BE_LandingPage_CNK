using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Branches.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Branches.Queries;

public record GetBranchDetailQuery(Guid Id) : IRequest<Result<BranchDetailDto>>;

public class GetBranchDetailQueryHandler : IRequestHandler<GetBranchDetailQuery, Result<BranchDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBranchDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BranchDetailDto>> Handle(GetBranchDetailQuery request, CancellationToken cancellationToken)
    {
        var branch = await _context.Branches
            .Include(x => x.BranchGalleries)
            .Include(x => x.BranchCoaches)
                .ThenInclude(bc => bc.Coach)
            .Include(x => x.StudentProfiles)
                .ThenInclude(sp => sp.User)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (branch == null)
            return Result<BranchDetailDto>.Failure("Branch not found");

        var stats = await _context.BranchStatsView.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        var dto = new BranchDetailDto
        {
            Id = branch.Id,
            Code = branch.Code,
            Name = branch.Name,
            ShortName = branch.ShortName,
            Address = branch.Address,
            Thumbnail = branch.Thumbnail,
            Area = branch.Area,
            Latitude = branch.Latitude,
            Longitude = branch.Longitude,
            Schedule = branch.Schedule,
            Fee = branch.Fee,
            IsFree = branch.IsFree,
            Description = branch.Description,
            IsActive = branch.IsActive,
            
            ActiveStudentCount = stats?.ActiveStudentCount ?? 0,
            HeadCoachIds = branch.BranchCoaches.Where(x => x.Title == CoachTitle.HeadCoach).Select(x => x.CoachId).ToList(),
            AssistantCoachIds = branch.BranchCoaches.Where(x => x.Title == CoachTitle.AssistantCoach).Select(x => x.CoachId).ToList(),

            Galleries = branch.BranchGalleries
                .Where(g => g.IsActive)
                .OrderBy(g => g.DisplayOrder)
                .Select(g => new BranchGalleryDto
                {
                    Id = g.Id,
                    MediaUrl = g.MediaUrl,
                    MediaType = g.MediaType.ToString(),
                    Caption = g.Caption,
                    DisplayOrder = g.DisplayOrder
                }).ToList(),

            Coaches = branch.BranchCoaches
                .Select(bc => new BranchCoachDto
                {
                    CoachId = bc.CoachId,
                    FullName = bc.Coach.FullName,
                    AvatarUrl = bc.Coach.AvatarUrl,
                    Title = bc.Title.ToString()
                }).ToList(),

            StudentLeaders = branch.StudentProfiles
                .Where(sp => sp.ClassRole == StudentClassRole.Monitor || sp.ClassRole == StudentClassRole.ViceMonitor)
                .Select(sp => new BranchStudentLeaderDto
                {
                    StudentId = sp.Id,
                    StudentCode = sp.StudentCode,
                    FullName = sp.User?.FullName ?? string.Empty, // Assuming User has FullName. Let's check User entity if we need to. Usually it's there.
                    Role = sp.ClassRole.ToString()
                }).ToList()
        };

        return Result<BranchDetailDto>.Success(dto);
    }
}
