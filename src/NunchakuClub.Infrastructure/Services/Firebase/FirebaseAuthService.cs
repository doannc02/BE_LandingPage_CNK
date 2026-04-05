using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using NunchakuClub.Application.Common.Interfaces;

namespace NunchakuClub.Infrastructure.Services.Firebase;

/// <summary>
/// Xác thực Firebase ID Token và đồng bộ Custom Claims bằng FirebaseAdmin SDK.
/// Singleton-safe — FirebaseAuth.DefaultInstance là singleton.
/// </summary>
public sealed class FirebaseAuthService : IFirebaseAuthService
{
    private readonly ILogger<FirebaseAuthService> _logger;

    public FirebaseAuthService(ILogger<FirebaseAuthService> logger)
    {
        _logger = logger;
    }

    public async Task<FirebaseTokenResult?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        if (FirebaseAdmin.FirebaseApp.DefaultInstance is null)
        {
            _logger.LogWarning("Firebase not initialized — cannot verify token");
            return null;
        }

        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, ct);

            decoded.Claims.TryGetValue("email", out var emailObj);
            decoded.Claims.TryGetValue("name", out var nameObj);
            decoded.Claims.TryGetValue("picture", out var pictureObj);

            return new FirebaseTokenResult(
                Uid: decoded.Uid,
                Email: emailObj?.ToString() ?? string.Empty,
                DisplayName: nameObj?.ToString(),
                PhotoUrl: pictureObj?.ToString());
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Firebase token verification failed: {Code}", ex.AuthErrorCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Firebase token verification");
            return null;
        }
    }

    public async Task SetCustomClaimsAsync(string uid, string role, CancellationToken ct = default)
    {
        if (FirebaseAdmin.FirebaseApp.DefaultInstance is null)
        {
            _logger.LogWarning("Firebase not initialized — skipping custom claims sync");
            return;
        }

        try
        {
            var claims = new Dictionary<string, object> { ["role"] = role };
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims);
            _logger.LogInformation("Set Firebase custom claim role={Role} for uid={Uid}", role, uid);
        }
        catch (Exception ex)
        {
            // Non-fatal: JWT vẫn hoạt động, chỉ RTDB rules bị lỗi sync
            _logger.LogWarning(ex, "Failed to set custom claims for Firebase uid={Uid}", uid);
        }
    }
}
