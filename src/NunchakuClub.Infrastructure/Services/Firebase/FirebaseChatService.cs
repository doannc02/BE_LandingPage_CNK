using System;
using System.IO;
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
