using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Users.DTOs;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Auth.Commands;

/// <summary>
/// Phân quyền cho người dùng khác — chỉ SuperAdmin được gọi endpoint này.
/// Sau khi cập nhật DB, tự động đồng bộ role xuống Firebase custom claims.
/// Trả về UserDetailDto sau khi cập nhật thành công.
/// </summary>
public record AssignRoleCommand(Guid TargetUserId, string Role) : IRequest<Result<UserDetailDto>>;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result<UserDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IFirebaseAuthService _firebaseAuthService;

    public AssignRoleCommandHandler(
        IApplicationDbContext context,
        IFirebaseAuthService firebaseAuthService)
    {
        _context = context;
        _firebaseAuthService = firebaseAuthService;
    }

    public async Task<Result<UserDetailDto>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var newRole))
            return Result<UserDetailDto>.Failure($"Role không hợp lệ: '{request.Role}'. Giá trị hợp lệ: SuperAdmin, SubAdmin, Student");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId, cancellationToken);

        if (user is null)
            return Result<UserDetailDto>.Failure("Không tìm thấy người dùng");

        user.Role = newRole;
        await _context.SaveChangesAsync(cancellationToken);

        // Đồng bộ role xuống Firebase nếu user có FirebaseUid
        if (!string.IsNullOrEmpty(user.FirebaseUid))
            await _firebaseAuthService.SetCustomClaimsAsync(user.FirebaseUid, newRole.ToString(), cancellationToken);

        return Result<UserDetailDto>.Success(new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            FullName = user.FullName,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            EmailVerified = user.EmailVerified,
            FirebaseUid = user.FirebaseUid,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }
}
