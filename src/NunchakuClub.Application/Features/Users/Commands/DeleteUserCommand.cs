using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.Users.Commands;

/// <summary>
/// Xoá người dùng khỏi hệ thống.
/// Chỉ SuperAdmin được gọi (enforce ở controller).
/// SuperAdmin không thể tự xóa chính mình.
/// </summary>
public record DeleteUserCommand(Guid UserId, Guid RequesterId) : IRequest<Result>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == request.RequesterId)
            return Result.Failure("Không thể xóa chính tài khoản đang đăng nhập");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("Không tìm thấy người dùng");

        // Không cho xóa SuperAdmin khác (chỉ có 1 SuperAdmin trong hệ thống)
        if (user.Role == NunchakuClub.Domain.Entities.UserRole.SuperAdmin)
            return Result.Failure("Không thể xóa tài khoản SuperAdmin");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
