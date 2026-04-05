using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Users.Commands;

/// <summary>
/// Thay đổi trạng thái tài khoản (Active / Inactive / Suspended).
/// Chỉ SuperAdmin và SubAdmin được gọi (enforce ở controller).
/// </summary>
public record UpdateUserStatusCommand(Guid UserId, string Status) : IRequest<Result>;

public class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserStatus>(request.Status, ignoreCase: true, out var newStatus))
            return Result.Failure($"Status không hợp lệ: '{request.Status}'. Giá trị hợp lệ: Active, Inactive, Suspended");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("Không tìm thấy người dùng");

        user.Status = newStatus;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
