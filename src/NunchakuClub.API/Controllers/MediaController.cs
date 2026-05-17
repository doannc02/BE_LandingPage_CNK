using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Media.Commands;
using NunchakuClub.Application.Features.Media.Queries;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Editor")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var result = await _mediator.Send(new UploadMediaCommand(file), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultiple(IFormFileCollection files, CancellationToken ct)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded");

        var result = await _mediator.Send(new UploadMultipleMediaCommand(files), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>
    /// Upload video bài học lên Cloudinary với tối ưu tự động.
    /// </summary>
    [HttpPost("upload-lesson-video")]
    [RequestSizeLimit(524_288_000)] // 500MB
    public async Task<IActionResult> UploadLessonVideo(
        [FromForm] IFormFile file,
        [FromForm] string lessonTitle,
        [FromForm] CourseLevel level,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Chưa chọn file video.");

        if (string.IsNullOrWhiteSpace(lessonTitle))
            return BadRequest("Tiêu đề bài học không được để trống.");

        var result = await _mediator.Send(new UploadLessonVideoCommand(file, lessonTitle, level), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>
    /// Tạo Signed URL có thời hạn để xem video bảo mật.
    /// </summary>
    [HttpGet("video-signed-url")]
    public async Task<IActionResult> GetVideoSignedUrl(
        [FromQuery] string publicId,
        [FromQuery] int expiresInMinutes = 60,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return BadRequest("publicId không được để trống.");

        if (expiresInMinutes is < 1 or > 1440)
            return BadRequest("expiresInMinutes phải từ 1 đến 1440 (tối đa 24 giờ).");

        var result = await _mediator.Send(new GetVideoSignedUrlQuery(publicId, expiresInMinutes), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>
    /// Xóa video bài học khỏi Cloudinary. Chỉ Admin mới có quyền.
    /// </summary>
    [HttpDelete("lesson-video")]
    [Authorize(Policy = "RequireAdminArea")]
    public async Task<IActionResult> DeleteLessonVideo(
        [FromQuery] string publicId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return BadRequest("publicId không được để trống.");

        var result = await _mediator.Send(new DeleteLessonVideoCommand(publicId), ct);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
