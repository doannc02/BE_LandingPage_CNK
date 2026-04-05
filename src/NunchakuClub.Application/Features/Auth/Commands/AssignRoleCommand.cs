using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Auth.Commands;

/// <summary>
/// Phân quyền cho người dùng khác — chỉ SuperAdmin được gọi endpoint này.
/// Sau khi cập nhật DB, tự động đồng bộ role xuống Firebase custom claims.
/// </summary>
public record AssignRoleCommand(Guid TargetUserId, string Role) : IRequest<Result>;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result>
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

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var newRole))
            return Result.Failure($"Role không hợp lệ: '{request.Role}'. Giá trị hợp lệ: SuperAdmin, SubAdmin, Student");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId, cancellationToken);

        if (user is null)
            return Result.Failure("Không tìm thấy người dùng");

        user.Role = newRole;
        await _context.SaveChangesAsync(cancellationToken);

        // Đồng bộ role xuống Firebase nếu user có FirebaseUid
        if (!string.IsNullOrEmpty(user.FirebaseUid))
            await _firebaseAuthService.SetCustomClaimsAsync(user.FirebaseUid, newRole.ToString(), cancellationToken);

        return Result.Success();
    }
}
