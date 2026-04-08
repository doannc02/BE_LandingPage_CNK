using System.Security.Claims;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
