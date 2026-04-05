using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Users.DTOs;

namespace NunchakuClub.Application.Features.Users.Queries;

/// <summary>
/// Lấy thông tin một người dùng theo ID.
/// Admin lấy được UserDetailDto (đầy đủ).
/// Student chỉ lấy được UserProfileDto của chính mình (enforce ở controller).
/// </summary>
public record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDetailDto>>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserDetailDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<UserDetailDto>.Failure("Không tìm thấy người dùng");

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
