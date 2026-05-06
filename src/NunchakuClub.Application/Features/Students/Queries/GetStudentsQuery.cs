using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Common.Extensions;
using NunchakuClub.Application.Features.Students.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NunchakuClub.Domain.Entities;
namespace NunchakuClub.Application.Features.Students.Queries;

public record GetStudentsQuery(
    Guid? BranchId = null,
    int PageNumber = 1,
    int PageSize = 20,
    Guid? BeltRankId = null,
    StudentLearningStatus? LearningStatus = null,
    StudentClassRole? ClassRole = null,
    DateTime? JoinDateFrom = null,
    DateTime? JoinDateTo = null,
    bool IsSortByName = false
) : IRequest<Result<PaginatedResult<StudentDto>>>;

public class GetStudentsQueryHandler : IRequestHandler<GetStudentsQuery, Result<PaginatedResult<StudentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedResult<StudentDto>>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.StudentProfiles
            .Include(x => x.User)
            .Include(x => x.Branch)
            .Include(x => x.CurrentBeltRank)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        // Filter by BranchId
        if (request.BranchId.HasValue)
            query = query.Where(x => x.BranchId == request.BranchId.Value);

        // Filter by BeltRankId
        if (request.BeltRankId.HasValue)
            query = query.Where(x => x.CurrentBeltRankId == request.BeltRankId.Value);

        // Filter by LearningStatus
        if (request.LearningStatus.HasValue)
            query = query.Where(x => x.LearningStatus == request.LearningStatus.Value);

        // Filter by ClassRole
        if (request.ClassRole.HasValue)
            query = query.Where(x => x.ClassRole == request.ClassRole.Value);

        // Filter by JoinDate range
        if (request.JoinDateFrom.HasValue)
            query = query.Where(x => x.JoinDate >= request.JoinDateFrom.Value);
        if (request.JoinDateTo.HasValue)
            query = query.Where(x => x.JoinDate <= request.JoinDateTo.Value);

        if (request.IsSortByName)
            query = query.OrderBy(x => x.StudentCode).ThenBy(x => x.User.FullName);
        else
            query = query.OrderByDescending(x => x.CreatedAt);

        var projected = query.Select(x => new StudentDto
        {
            Id = x.Id,
            UserId = x.UserId,
            UserFullName = x.User.FullName,
            UserEmail = x.User.Email,
            StudentCode = x.StudentCode,
            BranchId = x.BranchId,
            BranchName = x.Branch.Name,
            CurrentBeltRankId = x.CurrentBeltRankId,
            BeltRankName = x.CurrentBeltRank != null ? x.CurrentBeltRank.Name : null,
            Address = x.Address,
            HeightCm = x.HeightCm,
            WeightKg = x.WeightKg,
            Gender = x.Gender,
            DateOfBirth = x.DateOfBirth,
            JoinDate = x.JoinDate,
            LearningStatus = x.LearningStatus,
            ClassRole = x.ClassRole,
            GuardianName = x.GuardianName,
            GuardianPhone = x.GuardianPhone,
            Notes = x.Notes
        });

        var paginatedList = await projected.ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
        var paginatedResult = PaginatedResult<StudentDto>.FromPaginatedList(paginatedList);
        return Result<PaginatedResult<StudentDto>>.Success(paginatedResult);
    }
}
