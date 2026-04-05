using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Interfaces;

/// <summary>Kết quả verify Firebase ID Token.</summary>
public record FirebaseTokenResult(
    string Uid,
    string Email,
    string? DisplayName,
    string? PhotoUrl);

/// <summary>
/// Xác thực Firebase ID Token và đồng bộ Custom Claims.
/// Triển khai ở Infrastructure bằng FirebaseAdmin SDK.
/// </summary>
public interface IFirebaseAuthService
{
    /// <summary>
    /// Verify Firebase ID Token từ client.
    /// Trả về null nếu token không hợp lệ hoặc đã hết hạn.
    /// </summary>
    Task<FirebaseTokenResult?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);

    /// <summary>
    /// Ghi custom claim <c>role</c> lên Firebase Auth user.
    /// Frontend sẽ nhận được claim này qua auth.token.role trong Security Rules.
    /// </summary>
    Task SetCustomClaimsAsync(string uid, string role, CancellationToken ct = default);
}
