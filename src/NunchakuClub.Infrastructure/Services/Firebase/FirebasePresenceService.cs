using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NunchakuClub.Application.Common.Interfaces;

namespace NunchakuClub.Infrastructure.Services.Firebase;

/// <summary>
/// Đọc presence data từ Firebase Realtime Database qua REST API.
/// Singleton — GoogleCredential tự quản lý token refresh.
/// </summary>
public sealed class FirebasePresenceService : IFirebasePresenceService
{
    private readonly HttpClient _httpClient;
    private readonly FirebaseSettings _settings;
    private GoogleCredential? _credential;
    private readonly ILogger<FirebasePresenceService> _logger;

    private static readonly string[] FirebaseScopes =
    [
        "https://www.googleapis.com/auth/firebase.database",
        "https://www.googleapis.com/auth/userinfo.email"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FirebasePresenceService(
        IHttpClientFactory httpClientFactory,
        IOptions<FirebaseSettings> settings,
        ILogger<FirebasePresenceService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("firebase-presence");

        if (File.Exists(_settings.ServiceAccountPath))
        {
            using var stream = File.OpenRead(_settings.ServiceAccountPath);
#pragma warning disable CS0618 // GoogleCredential.FromStream vẫn hoạt động — deprecated chỉ advisory
            _credential = GoogleCredential
                .FromStream(stream)
                .CreateScoped(FirebaseScopes);
#pragma warning restore CS0618
        }
        else
        {
            _logger.LogWarning(
                "Firebase service account not found at '{Path}' — presence features disabled",
                _settings.ServiceAccountPath);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Trả về danh sách đầy đủ admin đang online (Online == true trong /presence/admins).
    /// Kết hợp với <see cref="IFirebaseChatService.GetAdminWorkloadsAsync"/> để chọn
    /// admin có ít phòng chat "open" nhất (chiến lược Least-Loaded).
    /// </remarks>
    public async Task<IReadOnlyList<OnlineAdmin>> GetOnlineAdminsAsync(CancellationToken ct = default)
    {
        var admins = await ReadAllPresenceAsync(ct);
        return admins.Values
            .Where(a => a.Online)
            .Select(a => new OnlineAdmin(a.AdminId, a.FcmToken, a.DisplayName))
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>Shortcut: lấy admin online đầu tiên — delegate sang <see cref="GetOnlineAdminsAsync"/>.</remarks>
    public async Task<OnlineAdmin?> GetFirstOnlineAdminAsync(CancellationToken ct = default)
    {
        var onlineAdmins = await GetOnlineAdminsAsync(ct);
        return onlineAdmins.FirstOrDefault();
    }

    public async Task<IReadOnlyList<string>> GetAllAdminFcmTokensAsync(CancellationToken ct = default)
    {
        var admins = await ReadAllPresenceAsync(ct);
        return admins.Values
            .Where(a => !string.IsNullOrWhiteSpace(a.FcmToken))
            .Select(a => a.FcmToken!)
            .Distinct()
            .ToList();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<Dictionary<string, AdminPresenceEntry>> ReadAllPresenceAsync(CancellationToken ct)
    {
        if (_credential is null)
        {
            _logger.LogDebug("Firebase not configured — returning empty presence");
            return new Dictionary<string, AdminPresenceEntry>();
        }

        try
        {
            var token = await GetAccessTokenAsync(ct);
            var url = $"{_settings.DatabaseUrl.TrimEnd('/')}/presence/admins.json?access_token={token}";

            var json = await _httpClient.GetStringAsync(url, ct);

            if (string.IsNullOrWhiteSpace(json) || json == "null")
                return new Dictionary<string, AdminPresenceEntry>();

            var raw = JsonSerializer.Deserialize<Dictionary<string, AdminPresenceRaw>>(json, JsonOptions);
            if (raw is null) return new Dictionary<string, AdminPresenceEntry>();

            return raw.ToDictionary(
                kv => kv.Key,
                kv => new AdminPresenceEntry(
                    AdminId: kv.Key,
                    Online: kv.Value.Online,
                    FcmToken: kv.Value.FcmToken,
                    DisplayName: kv.Value.DisplayName
                ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read admin presence from Firebase Realtime Database");
            return new Dictionary<string, AdminPresenceEntry>();
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var tokenAccess = (ITokenAccess)_credential!;
        return await tokenAccess.GetAccessTokenForRequestAsync(cancellationToken: ct);
    }

    // ── Private models ───────────────────────────────────────────────────────

    private sealed class AdminPresenceRaw
    {
        [JsonPropertyName("online")]
        public bool Online { get; set; }

        [JsonPropertyName("fcmToken")]
        public string? FcmToken { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("lastSeen")]
        public long? LastSeen { get; set; }
    }

    private sealed record AdminPresenceEntry(
        string AdminId,
        bool Online,
        string? FcmToken,
        string? DisplayName);
}
