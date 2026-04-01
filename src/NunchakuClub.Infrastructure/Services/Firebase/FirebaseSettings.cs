namespace NunchakuClub.Infrastructure.Services.Firebase;

public class FirebaseSettings
{
    /// <summary>Firebase Project ID, e.g. "my-project-abc123"</summary>
    public string ProjectId { get; set; } = null!;

    /// <summary>
    /// Path đến file service account JSON.
    /// Dev: "secrets/firebase-service-account.json"
    /// Production: đặt qua environment variable hoặc volume mount
    /// </summary>
    public string ServiceAccountPath { get; set; } = null!;

    /// <summary>Realtime Database URL, e.g. "https://my-project-abc123-default-rtdb.firebaseio.com"</summary>
    public string DatabaseUrl { get; set; } = null!;
}
