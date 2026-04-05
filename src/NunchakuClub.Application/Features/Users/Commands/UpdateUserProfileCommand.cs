using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Users.DTOs;

namespace NunchakuClub.Application.Features.Users.Commands;

/// <summary>
/// Cập nhật profile người dùng.
/// Student chỉ được cập nhật chính mình (enforce ở controller bằng cách so sánh requesterId == targetId).
/// Admin có thể cập nhật bất kỳ user nào.
/// </summary>
public record UpdateUserProfileCommand(
    Guid UserId,
    string FullName,
    string? Phone,
    string? AvatarUrl
) : IRequest<Result<UserProfileDto>>;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<UserProfileDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserProfileDto>> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<UserProfileDto>.Failure("Không tìm thấy người dùng");

        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result<UserProfileDto>.Failure("Họ tên không được để trống");

        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone?.Trim();
        if (request.AvatarUrl is not null)
            user.AvatarUrl = request.AvatarUrl;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<UserProfileDto>.Success(new UserProfileDto
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
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        });
    }
}
