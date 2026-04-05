using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Editor")]
public class MediaController : ControllerBase
{
    private readonly ICloudStorageService _cloudStorage;
    private readonly IVideoStorageService _videoStorage;

    public MediaController(ICloudStorageService cloudStorage, IVideoStorageService videoStorage)
    {
        _cloudStorage = cloudStorage;
        _videoStorage = videoStorage;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var result = await _cloudStorage.UploadAsync(file, file.FileName);
        
        return result.Success 
            ? Ok(new { url = result.Url, thumbnailUrl = result.ThumbnailUrl, fileName = result.FileName })
            : BadRequest(result.Error);
    }

    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultiple(List<IFormFile> files)
    {
        if (files == null || !files.Any())
            return BadRequest("No files uploaded");

        var results = new List<object>();

        foreach (var file in files)
        {
            var result = await _cloudStorage.UploadAsync(file, file.FileName);
            if (result.Success)
            {
                results.Add(new { url = result.Url, thumbnailUrl = result.ThumbnailUrl, fileName = result.FileName });
            }
        }

        return Ok(results);
    }

    /// <summary>
    /// Upload video bài học lên Cloudinary với tối ưu tự động.
    /// - Tự động nén và chuyển đổi sang mp4/webm.
    /// - Gắn tag theo level khóa học.
    /// - Trả về CDN URL cố định (lưu vào DB) và publicId (dùng cho Signed URL).
    /// </summary>
    [HttpPost("upload-lesson-video")]
    [RequestSizeLimit(524_288_000)] // 500MB
    public async Task<IActionResult> UploadLessonVideo(
        [FromForm] IFormFile file,
        [FromForm] string lessonTitle,
        [FromForm] CourseLevel level,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Chưa chọn file video.");

        if (string.IsNullOrWhiteSpace(lessonTitle))
            return BadRequest("Tiêu đề bài học không được để trống.");

        var result = await _videoStorage.UploadLessonVideoAsync(file, lessonTitle, level, cancellationToken);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            publicId     = result.PublicId,     // lưu vào DB — dùng để tạo Signed URL
            cdnUrl       = result.CdnUrl,        // link CDN cố định (video public)
            mp4Url       = result.Mp4Url,        // link mp4 đã tối ưu (có thể null nếu eager đang xử lý)
            webmUrl      = result.WebmUrl,       // link webm đã tối ưu
            thumbnailUrl = result.ThumbnailUrl,  // ảnh thumbnail bài học
            fileSizeMb   = Math.Round(result.FileSizeBytes / 1024.0 / 1024.0, 2),
            durationSec  = result.DurationSeconds
        });
    }

    /// <summary>
    /// Tạo Signed URL có thời hạn để xem video bảo mật.
    /// URL hết hạn sau expiresInMinutes phút — không thể chia sẻ lâu dài.
    /// </summary>
    [HttpGet("video-signed-url")]
    public async Task<IActionResult> GetVideoSignedUrl(
        [FromQuery] string publicId,
        [FromQuery] int expiresInMinutes = 60)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return BadRequest("publicId không được để trống.");

        if (expiresInMinutes is < 1 or > 1440)
            return BadRequest("expiresInMinutes phải từ 1 đến 1440 (tối đa 24 giờ).");

        var signedUrl = await _videoStorage.GetSignedVideoUrlAsync(publicId, expiresInMinutes);
        return Ok(new { signedUrl, expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes) });
    }

    /// <summary>
    /// Xóa video bài học khỏi Cloudinary. Chỉ Admin mới có quyền.
    /// </summary>
    [HttpDelete("lesson-video")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLessonVideo(
        [FromQuery] string publicId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return BadRequest("publicId không được để trống.");

        var success = await _videoStorage.DeleteVideoAsync(publicId, cancellationToken);
        return success ? NoContent() : BadRequest("Không thể xóa video. Kiểm tra lại publicId.");
    }
}
