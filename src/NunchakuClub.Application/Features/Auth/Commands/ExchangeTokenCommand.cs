using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Auth.DTOs;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Auth.Commands;

/// <summary>
/// SSO Identity Exchange — đổi Firebase ID Token lấy JWT nội bộ.
/// - User mới (chưa có trong DB): tạo với role Guest — admin phải cấp quyền Student sau.
/// - User đã tồn tại (khớp theo FirebaseUid hoặc Email): giữ nguyên role hiện tại.
///   Nếu user đã được admin cấp Student (hoặc cao hơn) thì role đó được đồng bộ xuống Firebase claims.
/// </summary>
public record ExchangeTokenCommand(string FirebaseIdToken) : IRequest<Result<AuthResponse>>;

public class ExchangeTokenCommandHandler : IRequestHandler<ExchangeTokenCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IFirebaseAuthService _firebaseAuthService;

    public ExchangeTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        IFirebaseAuthService firebaseAuthService)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _firebaseAuthService = firebaseAuthService;
    }

    public async Task<Result<AuthResponse>> Handle(ExchangeTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Firebase token
        var tokenResult = await _firebaseAuthService.VerifyIdTokenAsync(request.FirebaseIdToken, cancellationToken);
        if (tokenResult is null)
            return Result<AuthResponse>.Failure("Invalid or expired Firebase token");

        if (string.IsNullOrWhiteSpace(tokenResult.Email))
            return Result<AuthResponse>.Failure("Firebase token does not contain an email address");

        // 2. Tìm hoặc tạo user nội bộ
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.FirebaseUid == tokenResult.Uid || u.Email == tokenResult.Email,
            cancellationToken);

        if (user is null)
        {
            // Lần đầu đăng nhập Google — tạo với role Guest.
            // Admin phải dùng POST /api/users/{id}/assign-role để cấp Student.
            var username = SanitizeUsername(tokenResult.Email.Split('@')[0]);
            var usernameExists = await _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
            if (usernameExists)
                username = $"{username}_{tokenResult.Uid[..8]}";

            user = new User
            {
                Email = tokenResult.Email,
                Username = username,
                FullName = tokenResult.DisplayName ?? username,
                AvatarUrl = tokenResult.PhotoUrl,
                PasswordHash = string.Empty, // Firebase users không dùng password
                FirebaseUid = tokenResult.Uid,
                Role = UserRole.Guest,
                Status = UserStatus.Active,
                EmailVerified = true
            };

            _context.Users.Add(user);
        }
        else
        {
            // User đã tồn tại — giữ nguyên role (có thể đã được admin cấp Student/SubAdmin).
            // Nếu chưa có FirebaseUid (đăng ký bằng email/password trước) thì liên kết tài khoản Google.
            if (string.IsNullOrEmpty(user.FirebaseUid))
                user.FirebaseUid = tokenResult.Uid;
        }

        // 3. Phát refresh token
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // 4. Đồng bộ role xuống Firebase custom claims (cho RTDB Security Rules)
        await _firebaseAuthService.SetCustomClaimsAsync(tokenResult.Uid, user.Role.ToString(), cancellationToken);

        // 5. Phát JWT nội bộ
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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

    private static string SanitizeUsername(string raw)
    {
        // Chỉ giữ lại chữ, số, dấu gạch dưới
        var chars = new System.Text.StringBuilder();
        foreach (var c in raw)
            if (char.IsLetterOrDigit(c) || c == '_') chars.Append(c);
        var result = chars.ToString();
        return string.IsNullOrEmpty(result) ? "user" : result;
    }
}
