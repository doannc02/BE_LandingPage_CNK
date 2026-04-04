using Microsoft.AspNetCore.Http;
using NunchakuClub.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Interfaces;

public interface IVideoStorageService
{
    /// <summary>
    /// Upload và tối ưu video bài học lên Cloudinary.
    /// Tự động tạo eager transformations sang mp4/webm, nén chất lượng auto.
    /// Gắn tag theo level để dễ quản lý trên dashboard.
    /// </summary>
    Task<VideoUploadResult> UploadLessonVideoAsync(
        IFormFile file,
        string lessonTitle,
        CourseLevel level,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo Signed URL có thời hạn để bảo vệ video.
    /// URL chỉ hợp lệ trong khoảng thời gian expiresInMinutes.
    /// </summary>
    Task<string> GetSignedVideoUrlAsync(string publicId, int expiresInMinutes = 60);

    /// <summary>
    /// Xóa video khỏi Cloudinary theo publicId.
    /// </summary>
    Task<bool> DeleteVideoAsync(string publicId, CancellationToken cancellationToken = default);
}

public class VideoUploadResult
{
    public bool Success { get; set; }

    /// <summary>
    /// Cloudinary public_id — lưu vào database để generate signed URL sau này.
    /// Ví dụ: "lessons/beginner/beginner_bai-1_abc123"
    /// </summary>
    public string? PublicId { get; set; }

    /// <summary>
    /// CDN URL cố định (permanent) — link gốc, dùng khi video không cần bảo mật.
    /// </summary>
    public string? CdnUrl { get; set; }

    /// <summary>
    /// URL eager-generated sang định dạng mp4 (tối ưu cho web).
    /// Sẵn sàng sau khi eager processing hoàn tất.
    /// </summary>
    public string? Mp4Url { get; set; }

    /// <summary>
    /// URL eager-generated sang định dạng webm (tối ưu cho Chrome/Firefox).
    /// </summary>
    public string? WebmUrl { get; set; }

    /// <summary>
    /// URL ảnh thumbnail tự động generate từ giây đầu tiên của video.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    public long FileSizeBytes { get; set; }
    public double DurationSeconds { get; set; }
    public string? Error { get; set; }
}
