using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Services.Firebase;

/// <summary>
/// Gửi push notification đến admin qua Firebase Cloud Messaging (FCM).
///
/// Token resolution strategy (theo thứ tự ưu tiên):
///   1. Firebase Realtime Database presence (/presence/admins) — token tươi nhất, online admins
///   2. PostgreSQL users.fcm_token — fallback khi admin offline / Firebase presence trống
///
/// Vì service này là Singleton, dùng IServiceScopeFactory để truy cập Scoped IApplicationDbContext.
/// </summary>
public sealed class FcmNotificationService : IFcmNotificationService
{
    private readonly IFirebasePresenceService _presence;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FcmNotificationService> _logger;

    public FcmNotificationService(
        IFirebasePresenceService presence,
        IServiceScopeFactory scopeFactory,
        ILogger<FcmNotificationService> logger)
    {
        _presence = presence;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task NotifyAllAdminsAsync(string userMessage, Guid pendingMessageId, CancellationToken ct = default)
    {
        // 1. Primary: FCM tokens từ Firebase presence (admin đang online)
        var tokens = await _presence.GetAllAdminFcmTokensAsync(ct);

        // 2. Fallback: tokens từ PostgreSQL (admin đã đăng ký nhưng offline)
        if (tokens.Count == 0)
        {
            _logger.LogInformation("Firebase presence has no FCM tokens — falling back to PostgreSQL");
            tokens = await GetPgFcmTokensAsync(ct);
        }

        if (tokens.Count == 0)
        {
            _logger.LogWarning("NotifyAllAdmins: no FCM tokens found in Firebase or PostgreSQL — notification skipped");
            return;
        }

        if (FirebaseMessaging.DefaultInstance is null)
        {
            _logger.LogWarning("FirebaseMessaging not initialized — push notification skipped");
            return;
        }

        var preview = userMessage.Length > 100
            ? string.Concat(userMessage.AsSpan(0, 100), "...")
            : userMessage;

        var message = new MulticastMessage
        {
            Notification = new Notification
            {
                Title = "Tin nhắn mới từ người dùng",
                Body = preview,
            },
            Data = new Dictionary<string, string>
            {
                ["pendingMessageId"] = pendingMessageId.ToString(),
                ["url"] = "/admin/messages",
                ["type"] = "pending_message",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
            },
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                    Sound = "default"
                }
            },
            Webpush = new WebpushConfig
            {
                Notification = new WebpushNotification
                {
                    Title = "Tin nhắn mới từ người dùng",
                    Body = preview,
                    Icon = "/logo.png",
                }
            },
            Tokens = (IReadOnlyList<string>)tokens,
        };

        try
        {
            var result = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, ct);

            _logger.LogInformation(
                "FCM multicast: {Total} tokens, {Success} success, {Failed} failed",
                tokens.Count, result.SuccessCount, result.FailureCount);

            if (result.FailureCount > 0)
                LogFailures(result.Responses, tokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM SendEachForMulticast threw an exception");
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Lấy FCM tokens của tất cả Admin/Editor từ PostgreSQL.
    /// Tạo scope mới vì IApplicationDbContext là Scoped, còn service này là Singleton.
    /// </summary>
    private async Task<IReadOnlyList<string>> GetPgFcmTokensAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            return await db.Users
                .Where(u => u.FcmToken != null
                         && (u.Role == UserRole.Admin || u.Role == UserRole.Editor))
                .Select(u => u.FcmToken!)
                .Distinct()
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read FCM tokens from PostgreSQL");
            return [];
        }
    }

    private void LogFailures(IReadOnlyList<SendResponse> responses, IReadOnlyList<string> tokens)
    {
        for (var i = 0; i < responses.Count; i++)
        {
            if (!responses[i].IsSuccess)
            {
                _logger.LogWarning(
                    "FCM failed for token [{Index}]: {Error}",
                    i, responses[i].Exception?.Message);
            }
        }
    }
}
