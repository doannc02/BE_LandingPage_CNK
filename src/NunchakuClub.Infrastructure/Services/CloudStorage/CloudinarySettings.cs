namespace NunchakuClub.Infrastructure.Services.CloudStorage;

public class CloudinarySettings
{
    /// <summary>Cloudinary cloud name (dashboard → Settings → Account).</summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>API Key — dashboard → Settings → Access Keys.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>API Secret — dashboard → Settings → Access Keys.</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Auth Token Key dùng để ký Signed URL.
    /// Dashboard → Settings → Security → Auth Token → Token Key.
    /// </summary>
    public string AuthTokenKey { get; set; } = string.Empty;

    /// <summary>
    /// true  → video upload với type="authenticated" (chỉ xem qua Signed URL).
    /// false → video upload với type="upload" (public CDN, dùng CDN URL trực tiếp).
    /// </summary>
    public bool UsePrivateDelivery { get; set; } = false;
}
