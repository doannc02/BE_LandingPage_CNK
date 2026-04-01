using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Services.Firebase;

/// <summary>
/// BackgroundService chạy định kỳ để retry gửi FCM notification cho các pending messages
/// chưa được admin xử lý.
///
/// Retry strategy (exponential back-off):
///   Attempt 0 → gửi ngay lúc tạo (ProcessChatHandler)
///   Attempt 1 → retry sau 5 phút
///   Attempt 2 → retry sau 15 phút
///   Attempt 3 → retry sau 60 phút
///   Attempt ≥ 4 → không retry nữa (admin sẽ thấy trong dashboard)
///
/// Service này là Singleton (BackgroundService lifecycle); dùng IServiceScopeFactory
/// để tạo Scoped IApplicationDbContext và IFcmNotificationService mỗi lần chạy.
/// </summary>
public sealed class NotificationRetryService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    // Delay (minutes) sau mỗi lần retry thất bại
    private static readonly int[] RetryDelayMinutes = [5, 15, 60];
    private const int MaxRetries = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationRetryService> _logger;

    public NotificationRetryService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationRetryService started — checking every {Interval} minutes",
            CheckInterval.TotalMinutes);

        // Delay nhỏ khi startup để DB migration hoàn tất trước
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunRetryPassAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunRetryPassAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var fcm = scope.ServiceProvider.GetRequiredService<IFcmNotificationService>();

            var now = DateTime.UtcNow;

            // Lấy tất cả pending messages cần retry:
            // - Status còn Pending (chưa có admin nào nhận/reply)
            // - NextNotificationAt đã đến hoặc chưa set
            // - Chưa vượt quá MaxRetries
            var messages = await db.PendingUserMessages
                .Where(m => m.Status == PendingMessageStatus.Pending
                         && m.NotificationRetryCount < MaxRetries
                         && (m.NextNotificationAt == null || m.NextNotificationAt <= now))
                .ToListAsync(ct);

            if (messages.Count == 0)
            {
                _logger.LogDebug("NotificationRetry: no messages due for retry");
                return;
            }

            _logger.LogInformation("NotificationRetry: found {Count} message(s) to retry", messages.Count);

            foreach (var msg in messages)
            {
                try
                {
                    await fcm.NotifyAllAdminsAsync(msg.UserMessage, msg.Id, ct);

                    msg.NotificationRetryCount += 1;
                    var delayMinutes = msg.NotificationRetryCount < RetryDelayMinutes.Length
                        ? RetryDelayMinutes[msg.NotificationRetryCount - 1]
                        : RetryDelayMinutes[^1];
                    msg.NextNotificationAt = now.AddMinutes(delayMinutes);

                    _logger.LogInformation(
                        "Retry #{Count} sent for message {Id}, next retry in {Delay} min",
                        msg.NotificationRetryCount, msg.Id, delayMinutes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retry notification for message {Id}", msg.Id);
                }
            }

            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // App is shutting down — normal
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotificationRetryService pass failed");
        }
    }
}
