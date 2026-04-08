using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Auth.DTOs;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<Result<AuthResponse>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            return Result<AuthResponse>.Failure("Invalid access token");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Result<AuthResponse>.Failure("Invalid access token claims");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result<AuthResponse>.Failure("User not found");

        if (user.RefreshToken != request.RefreshToken)
            return Result<AuthResponse>.Failure("Invalid refresh token");

        if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Result<AuthResponse>.Failure("Refresh token has expired");

        if (user.Status != UserStatus.Active)
            return Result<AuthResponse>.Failure("Account is not active");

        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl
            }
        });
    }
}
