using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NunchakuClub.Application.Common.Interfaces;

namespace NunchakuClub.Infrastructure.Services.Firebase;

/// <summary>
/// Ghi chat room + messages vào Firebase Realtime Database qua REST API.
/// Singleton — credential được tạo một lần khi khởi tạo.
/// </summary>
public sealed class FirebaseChatService : IFirebaseChatService
{
    private readonly HttpClient _http;
    private readonly FirebaseSettings _settings;
    private GoogleCredential? _credential;
    private readonly ILogger<FirebaseChatService> _logger;

    private static readonly string[] FirebaseScopes =
    [
        "https://www.googleapis.com/auth/firebase.database",
        "https://www.googleapis.com/auth/userinfo.email"
    ];

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FirebaseChatService(
        IHttpClientFactory httpClientFactory,
        IOptions<FirebaseSettings> settings,
        ILogger<FirebaseChatService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _http = httpClientFactory.CreateClient("firebase-presence");

        if (File.Exists(_settings.ServiceAccountPath))
        {
            using var stream = File.OpenRead(_settings.ServiceAccountPath);
#pragma warning disable CS0618
            _credential = GoogleCredential.FromStream(stream).CreateScoped(FirebaseScopes);
#pragma warning restore CS0618
        }
        else
        {
            _logger.LogWarning(
                "Firebase service account not found at '{Path}' — chat room creation disabled",
                _settings.ServiceAccountPath);
        }
    }

    // ── Least-Loaded: Admin Workload Query ────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Truy vấn Firebase REST API với index <c>metadata/status</c> (cấu hình trong firebase.rules).
    /// Chỉ trả về các chat rooms có <c>status == "open"</c>, sau đó GroupBy <c>adminId</c>
    /// để đếm số phòng đang xử lý của từng admin.
    /// </para>
    /// <para>
    /// Admin nào không xuất hiện trong kết quả → workload = 0 (đang rảnh hoàn toàn).
    /// </para>
    /// </remarks>
    public async Task<Dictionary<string, int>> GetAdminWorkloadsAsync(CancellationToken ct = default)
    {
        if (_credential is null)
        {
            _logger.LogDebug("Firebase not configured — returning empty workloads");
            return new Dictionary<string, int>();
        }

        try
        {
            var token = await GetTokenAsync(ct);

            // REST query sử dụng index "metadata/status" để chỉ lấy chat rooms đang open
            // Ref: https://firebase.google.com/docs/database/rest/retrieve-data#section-rest-filtering
            var url = $"{_settings.DatabaseUrl.TrimEnd('/')}/chats.json"
                    + $"?orderBy=\"metadata/status\"&equalTo=\"open\"&access_token={token}";

            var json = await _http.GetStringAsync(url, ct);

            if (string.IsNullOrWhiteSpace(json) || json == "null")
            {
                _logger.LogDebug("No open chats found in Firebase — all admins have zero workload");
                return new Dictionary<string, int>();
            }

            // Parse response: { "chat_xxx": { "metadata": { "adminId": "...", ... }, ... }, ... }
            using var doc = JsonDocument.Parse(json);
            var workloads = new Dictionary<string, int>();

            foreach (var chatEntry in doc.RootElement.EnumerateObject())
            {
                // Mỗi chatEntry.Value là một chat room, trích xuất metadata.adminId
                if (chatEntry.Value.TryGetProperty("metadata", out var metadata) &&
                    metadata.TryGetProperty("adminId", out var adminIdProp))
                {
                    var adminId = adminIdProp.GetString();
                    if (!string.IsNullOrEmpty(adminId))
                    {
                        workloads[adminId] = workloads.GetValueOrDefault(adminId, 0) + 1;
                    }
                }
            }

            _logger.LogDebug(
                "Admin workloads from Firebase RTDB: {Workloads}",
                string.Join(", ", workloads.Select(kv => $"{kv.Key}={kv.Value}")));

            return workloads;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query admin workloads from Firebase RTDB");
            // Trả về rỗng thay vì throw — caller vẫn có thể fallback chọn admin đầu tiên
            return new Dictionary<string, int>();
        }
    }

    public async Task SendMessageAsync(
        string chatId,
        string sender,
        string text,
        CancellationToken ct = default)
    {
        if (_credential is null)
        {
            _logger.LogWarning("Firebase not configured — SendMessage skipped for {ChatId}", chatId);
            return;
        }

        try
        {
            var token = await GetTokenAsync(ct);
            await PostAsync(
                $"chats/{chatId}/messages",
                new
                {
                    sender,
                    text,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    read = false
                },
                token, ct);

            _logger.LogInformation("Message sent to chat {ChatId} by {Sender}", chatId, sender);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessageAsync failed for chat {ChatId}", chatId);
        }
    }

    public async Task CloseChatAsync(string chatId, CancellationToken ct = default)
    {
        if (_credential is null) return;

        try
        {
            var token = await GetTokenAsync(ct);
            await PatchAsync(
                $"chats/{chatId}/metadata",
                new
                {
                    status = "closed",
                    closedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                },
                token, ct);

            _logger.LogInformation("Chat room {ChatId} closed", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CloseChatAsync failed for {ChatId}", chatId);
        }
    }

    public async Task NotifyUserReplyAsync(
        string sessionId,
        string adminReply,
        string adminId,
        CancellationToken ct = default)
    {
        if (_credential is null) return;

        try
        {
            var token = await GetTokenAsync(ct);
            // Overwrite với PUT — user frontend subscribe onValue tại path này
            await PutAsync(
                $"notifications/{sessionId}",
                new
                {
                    reply = adminReply,
                    adminId,
                    repliedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    read = false
                },
                token, ct);

            _logger.LogInformation("Reply notification written for session {Session}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotifyUserReplyAsync failed for session {Session}", sessionId);
        }
    }

    public async Task<string> CreateChatRoomAsync(
        string sessionId,
        string adminId,
        string firstMessage,
        CancellationToken ct = default)
    {
        var chatId = BuildChatId();

        if (_credential is null)
        {
            _logger.LogWarning("Firebase not configured — returning placeholder chatRoomId");
            return chatId;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            var token = await GetTokenAsync(ct);

            // Write metadata
            await PutAsync(
                $"chats/{chatId}/metadata",
                new
                {
                    userId = sessionId,
                    adminId,
                    status = "open",
                    userQuestion = firstMessage,
                    createdAt = now
                },
                token, ct);

            // Push first user message
            await PostAsync(
                $"chats/{chatId}/messages",
                new
                {
                    sender = "user",
                    text = firstMessage,
                    timestamp = now,
                    read = false
                },
                token, ct);

            _logger.LogInformation(
                "Firebase chat room created: {ChatId} (session={Session}, admin={Admin})",
                chatId, sessionId, adminId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Firebase chat room {ChatId}", chatId);
            // Return chatId anyway — frontend can still render optimistically
        }

        return chatId;
    }

    // ── REST helpers ──────────────────────────────────────────────────────────

    private async Task PutAsync(string path, object data, string token, CancellationToken ct)
    {
        var url = $"{_settings.DatabaseUrl.TrimEnd('/')}/{path}.json?access_token={token}";
        using var content = JsonContent.Create(data, options: JsonOpts);
        var response = await _http.PutAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task PostAsync(string path, object data, string token, CancellationToken ct)
    {
        var url = $"{_settings.DatabaseUrl.TrimEnd('/')}/{path}.json?access_token={token}";
        using var content = JsonContent.Create(data, options: JsonOpts);
        var response = await _http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>PATCH = partial update (Firebase equivalent of update()).</summary>
    private async Task PatchAsync(string path, object data, string token, CancellationToken ct)
    {
        var url = $"{_settings.DatabaseUrl.TrimEnd('/')}/{path}.json?access_token={token}";
        using var content = JsonContent.Create(data, options: JsonOpts);
        using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        var tokenAccess = (ITokenAccess)_credential!;
        return await tokenAccess.GetAccessTokenForRequestAsync(cancellationToken: ct);
    }

    /// <summary>Format: chat_{timestamp}_{8-char random} — URL-safe, sortable by time.</summary>
    private static string BuildChatId()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rand = Guid.NewGuid().ToString("N")[..8];
        return $"chat_{ts}_{rand}";
    }
}
