using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// Alias để tránh conflict với CloudinaryDotNet.Actions.VideoUploadResult
using AppVideoUploadResult = NunchakuClub.Application.Common.Interfaces.VideoUploadResult;

namespace NunchakuClub.Infrastructure.Services.CloudStorage;

public class CloudinaryVideoService : IVideoStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;
    private readonly ILogger<CloudinaryVideoService> _logger;

    // Giới hạn upload: 500MB
    private const long MaxVideoSizeBytes = 500 * 1024 * 1024;
    // Chunk size cho UploadLarge: 20MB (Cloudinary khuyến nghị)
    private const int ChunkSizeBytes = 20 * 1024 * 1024;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/webm", "video/ogg", "video/quicktime",
        "video/x-msvideo", "video/x-matroska", "video/mpeg"
    };

    public CloudinaryVideoService(IOptions<CloudinarySettings> settings, ILogger<CloudinaryVideoService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    public async Task<AppVideoUploadResult> UploadLessonVideoAsync(
        IFormFile file,
        string lessonTitle,
        CourseLevel level,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return Fail("File không hợp lệ hoặc rỗng.");

        if (file.Length > MaxVideoSizeBytes)
            return Fail($"Video vượt quá giới hạn {MaxVideoSizeBytes / 1024 / 1024}MB.");

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return Fail($"Định dạng file '{file.ContentType}' không được hỗ trợ.");

        var levelTag = level.ToString().ToLower();           // "beginner", "intermediate", ...
        var slug     = Slugify(lessonTitle);
        var publicId = $"lessons/{levelTag}/{levelTag}_{slug}_{Guid.NewGuid():N}";

        // ── Eager transformations ─────────────────────────────────────────────
        // EagerAsync=true → Cloudinary xử lý nền, không block request upload.
        // FetchFormat("mp4") / FetchFormat("webm") → tự động encode sang format tối ưu web.
        // Quality("auto:good") → Cloudinary chọn bitrate thông minh, giảm ~40-70% dung lượng.
        // VideoCodec("auto") → H.264 cho mp4, VP9 cho webm.
        var eagerTransforms = new List<Transformation>
        {
            new EagerTransformation().Quality("auto:good").VideoCodec("auto").FetchFormat("mp4"),
            new EagerTransformation().Quality("auto:good").VideoCodec("auto").FetchFormat("webm")
        };

        using var stream = file.OpenReadStream();

        var uploadParams = new VideoUploadParams
        {
            File            = new FileDescription(file.FileName, stream),
            PublicId        = publicId,
            Overwrite       = false,
            UniqueFilename  = false,
            Tags            = $"{levelTag},lesson,nunchaku-club",
            EagerTransforms = eagerTransforms,
            EagerAsync      = true,   // xử lý eager nền — upload trả về ngay lập tức

            // "authenticated" → video chỉ xem được qua Signed URL (bảo mật hơn).
            // "upload" → public CDN (dùng khi không cần bảo vệ nội dung).
            Type = _settings.UsePrivateDelivery ? "authenticated" : "upload",
        };

        try
        {
            // UploadLargeAsync xử lý cả file nhỏ lẫn lớn qua chunked upload.
            // Với file < ChunkSize, hoạt động như UploadAsync bình thường.
            var uploadResult = await _cloudinary.UploadLargeAsync(uploadParams, ChunkSizeBytes, cancellationToken);

            if (uploadResult.Error is not null)
            {
                _logger.LogError("Cloudinary upload error: {Msg}", uploadResult.Error.Message);
                return Fail(uploadResult.Error.Message);
            }

            // ── Xây dựng URLs ─────────────────────────────────────────────────
            // CDN URL cố định — không thay đổi theo thời gian → lưu vào DB
            var cdnUrl = uploadResult.SecureUrl?.ToString();

            // Eager URLs được xây deterministic từ transform — sẵn sàng khi eager xong.
            // URL có dạng: https://res.cloudinary.com/{cloud}/video/upload/q_auto:good,vc_auto/f_mp4/{publicId}.mp4
            var mp4Transform  = new Transformation().Quality("auto:good").VideoCodec("auto").FetchFormat("mp4");
            var webmTransform = new Transformation().Quality("auto:good").VideoCodec("auto").FetchFormat("webm");

            var mp4Url  = _cloudinary.Api.UrlVideoUp.Transform(mp4Transform).BuildUrl(publicId);
            var webmUrl = _cloudinary.Api.UrlVideoUp.Transform(webmTransform).BuildUrl(publicId);

            // Thumbnail: frame đầu tiên của video, resize 640x360 (16:9), định dạng jpg
            var thumbTransform = new Transformation()
                .Width(640).Height(360).Crop("fill").StartOffset("0");

            var thumbnailUrl = _cloudinary.Api.UrlImgUp
                .ResourceType("video")
                .Transform(thumbTransform)
                .Format("jpg")
                .BuildUrl(publicId);

            _logger.LogInformation(
                "Video uploaded: PublicId={PublicId}, Size={SizeKB}KB, Duration={Duration}s, Level={Level}",
                publicId, uploadResult.Length / 1024, uploadResult.Duration, level);

            return new AppVideoUploadResult
            {
                Success         = true,
                PublicId        = publicId,
                CdnUrl          = cdnUrl,
                Mp4Url          = mp4Url,
                WebmUrl         = webmUrl,
                ThumbnailUrl    = thumbnailUrl,
                FileSizeBytes   = uploadResult.Length,
                DurationSeconds = uploadResult.Duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading video to Cloudinary");
            return Fail(ex.Message);
        }
    }

    public Task<string> GetSignedVideoUrlAsync(string publicId, int expiresInMinutes = 60)
    {
        // ── Cơ chế Signed URL (AuthToken) ────────────────────────────────────
        //
        // Cloudinary ký URL bằng HMAC-SHA256 với AuthTokenKey.
        // URL sinh ra có query string "__cld_token__=..." chứa:
        //   - exp  : unix timestamp hết hạn
        //   - acl  : access control path (wildcard)
        //   - hmac : chữ ký HMAC của (exp + acl)
        //
        // Khi CDN nhận request:
        //   1. Kiểm tra exp — nếu hết hạn → 403 Forbidden
        //   2. Kiểm tra acl — nếu path không match → 403 Forbidden
        //   3. Verify hmac — nếu sai → 403 Forbidden
        //
        // ── Domain Restriction ────────────────────────────────────────────────
        // SDK không enforce domain trực tiếp. Để giới hạn domain:
        //   Dashboard → Settings → Security → Allowed fetch domains
        //   Thêm domain: https://nunchakuclub.vn (và www.nunchakuclub.vn)
        // Sau đó CDN sẽ reject request từ domain không được phép (Referer check).
        //
        // Ngoài ra có thể dùng Acl với url_prefix để restrict path:
        //   .Acl("/*/lessons/*") — chỉ cho phép resource trong folder lessons

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes).ToUnixTimeSeconds();

        var authToken = new AuthToken(_settings.AuthTokenKey)
            .Expiration(expiresAt)
            .Acl("/*/lessons/*");   // chỉ video trong folder lessons mới được phép

        var deliveryType = _settings.UsePrivateDelivery ? "authenticated" : "upload";

        var signedUrl = _cloudinary.Api.UrlVideoUp
            .Signed(true)
            .AuthToken(authToken)
            .Format("mp4")
            .BuildUrl(publicId);

        _logger.LogDebug(
            "Signed URL generated for {PublicId}, expires at {ExpiresAt} ({Minutes}m)",
            publicId, DateTimeOffset.FromUnixTimeSeconds(expiresAt), expiresInMinutes);

        return Task.FromResult(signedUrl);
    }

    public async Task<bool> DeleteVideoAsync(string publicId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Video,
                Type         = _settings.UsePrivateDelivery ? "authenticated" : "upload",
                Invalidate   = true   // xóa cache CDN ngay lập tức
            };

            var result = await _cloudinary.DestroyAsync(deleteParams);
            var success = result.Result == "ok";

            if (!success)
                _logger.LogWarning("Cloudinary delete non-ok: {Result} for {PublicId}", result.Result, publicId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting video {PublicId} from Cloudinary", publicId);
            return false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppVideoUploadResult Fail(string error) =>
        new() { Success = false, Error = error };

    /// <summary>Chuyển tiêu đề bài học thành URL-safe slug.</summary>
    private static string Slugify(string title)
    {
        var s = title.ToLower().Trim();
        s = Regex.Replace(s, @"[àáạảãâầấậẩẫăằắặẳẵ]", "a");
        s = Regex.Replace(s, @"[èéẹẻẽêềếệểễ]", "e");
        s = Regex.Replace(s, @"[ìíịỉĩ]", "i");
        s = Regex.Replace(s, @"[òóọỏõôồốộổỗơờớợởỡ]", "o");
        s = Regex.Replace(s, @"[ùúụủũưừứựửữ]", "u");
        s = Regex.Replace(s, @"[ỳýỵỷỹ]", "y");
        s = Regex.Replace(s, @"đ", "d");
        s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"-+", "-").Trim('-');
        return s.Length > 50 ? s[..50] : s;
    }
}
