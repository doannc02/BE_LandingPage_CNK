using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Interfaces;

public interface IFirebasePresenceService
{
    /// <summary>Trả về admin đầu tiên đang online, hoặc null nếu không có ai.</summary>
    Task<OnlineAdmin?> GetFirstOnlineAdminAsync(CancellationToken ct = default);

    /// <summary>
    /// Trả về danh sách tất cả admin đang online.
    /// Dùng cho chiến lược Least-Loaded: kết hợp với GetAdminWorkloadsAsync
    /// để chọn admin có ít phòng chat nhất.
    /// </summary>
    Task<IReadOnlyList<OnlineAdmin>> GetOnlineAdminsAsync(CancellationToken ct = default);

    /// <summary>Trả về tất cả FCM tokens của admin (kể cả offline) để gửi push notification.</summary>
    Task<IReadOnlyList<string>> GetAllAdminFcmTokensAsync(CancellationToken ct = default);
}

public record OnlineAdmin(string AdminId, string? FcmToken, string? DisplayName);
