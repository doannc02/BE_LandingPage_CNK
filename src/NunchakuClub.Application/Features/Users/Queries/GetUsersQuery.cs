using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Extensions;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Users.DTOs;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Users.Queries;

/// <summary>
/// Lấy danh sách người dùng có phân trang + filter.
/// Chỉ SuperAdmin và SubAdmin được gọi (enforce ở controller).
/// </summary>
public record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? Role = null,
    string? Status = null
) : IRequest<Result<PaginatedList<UserDetailDto>>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PaginatedList<UserDetailDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<UserDetailDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        // Search by email, username, fullname
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                u.Username.ToLower().Contains(term) ||
                u.FullName.ToLower().Contains(term));
        }

        // Filter by role
        if (!string.IsNullOrWhiteSpace(request.Role) &&
            System.Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var roleFilter))
        {
            query = query.Where(u => u.Role == roleFilter);
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            System.Enum.TryParse<UserStatus>(request.Status, ignoreCase: true, out var statusFilter))
        {
            query = query.Where(u => u.Status == statusFilter);
        }

        query = query.OrderByDescending(u => u.CreatedAt);

        var projected = query.Select(u => new UserDetailDto
        {
            Id = u.Id,
            Email = u.Email,
            Username = u.Username,
            FullName = u.FullName,
            Phone = u.Phone,
            AvatarUrl = u.AvatarUrl,
            Role = u.Role.ToString(),
            Status = u.Status.ToString(),
            EmailVerified = u.EmailVerified,
            FirebaseUid = u.FirebaseUid,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        });

        var result = await projected.ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return Result<PaginatedList<UserDetailDto>>.Success(result);
    }
}
