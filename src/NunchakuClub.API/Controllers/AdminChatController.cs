using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.AdminChat.Commands;
using NunchakuClub.Application.Features.AdminChat.Queries;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.API.Controllers;

/// <summary>
/// Endpoints dành riêng cho SuperAdmin/SubAdmin để quản lý chat support.
///
/// Luồng A — Admin offline khi user gửi tin:
///   1. User gửi message → AI không đủ confidence → Admin offline
///   2. Message lưu vào pending_user_messages (PostgreSQL) + FCM push
///   3. Admin mở dashboard → GET /pending-messages → reply
///   4. Reply được ghi vào Firebase /notifications/{sessionId} → user thấy ngay
///
/// Luồng B — Admin online khi user gửi tin:
///   1. User gửi message → AI không đủ confidence → Admin online
///   2. Firebase chat room được tạo, admin thấy qua Firebase subscription
///   3. Admin gửi message qua POST /chats/{chatId}/message (hoặc Firebase SDK trực tiếp)
///   4. Admin đóng cuộc chat qua POST /chats/{chatId}/close
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "RequireAdminArea")]
public sealed class AdminChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string AdminId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── FCM token management ──────────────────────────────────────────────────

    /// <summary>
    /// POST /api/admin/fcm-token
    /// Admin gửi FCM device token sau khi đăng nhập.
    /// </summary>
    [HttpPost("fcm-token")]
    public async Task<IActionResult> SaveFcmToken(
        [FromBody] SaveFcmTokenRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return BadRequest("FCM token không được rỗng.");

        if (!Guid.TryParse(AdminId, out var adminGuid))
            return Unauthorized("Không xác định được admin ID.");

        var result = await _mediator.Send(new SaveFcmTokenCommand(adminGuid, req.Token.Trim()), ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // ── Pending messages (luồng A — admin offline) ───────────────────────────

    /// <summary>
    /// GET /api/admin/pending-messages
    /// </summary>
    [HttpGet("pending-messages")]
    public async Task<IActionResult> GetPendingMessages(
        [FromQuery] PendingMessageStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPendingMessagesQuery(status, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>
    /// POST /api/admin/pending-messages/{id}/reply
    /// </summary>
    [HttpPost("pending-messages/{id:guid}/reply")]
    public async Task<IActionResult> ReplyPendingMessage(
        Guid id,
        [FromBody] ReplyPendingMessageRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest("Nội dung reply không được rỗng.");

        var result = await _mediator.Send(new ReplyPendingMessageCommand(id, AdminId, req.Text.Trim()), ct);
        return result.IsSuccess ? Ok() : (result.Error!.Contains("không tìm thấy") ? NotFound(result.Error) : BadRequest(result.Error));
    }

    // ── Notification badge ────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/admin/pending-count
    /// </summary>
    [HttpGet("pending-count")]
    public async Task<IActionResult> GetPendingCount(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPendingCountQuery(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>
    /// POST /api/admin/pending-messages/{id}/close
    /// </summary>
    [HttpPost("pending-messages/{id:guid}/close")]
    public async Task<IActionResult> ClosePendingMessage(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ClosePendingMessageCommand(id), ct);
        return result.IsSuccess ? Ok() : (result.Error!.Contains("không tìm thấy") ? NotFound(result.Error) : BadRequest(result.Error));
    }

    // ── Active Firebase chat rooms (luồng B — admin online) ──────────────────

    /// <summary>
    /// POST /api/admin/chats/{chatId}/message
    /// </summary>
    [HttpPost("chats/{chatId}/message")]
    public async Task<IActionResult> SendChatMessage(
        string chatId,
        [FromBody] SendChatMessageRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest("Nội dung message không được rỗng.");

        var result = await _mediator.Send(new SendAdminChatMessageCommand(chatId, AdminId, req.Text.Trim()), ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    /// <summary>
    /// POST /api/admin/chats/{chatId}/close
    /// </summary>
    [HttpPost("chats/{chatId}/close")]
    public async Task<IActionResult> CloseChat(string chatId, CancellationToken ct)
    {
        var result = await _mediator.Send(new CloseChatCommand(chatId), ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────

    public sealed class SaveFcmTokenRequest
    {
        public string Token { get; init; } = string.Empty;
    }

    public sealed class ReplyPendingMessageRequest
    {
        public string Text { get; init; } = string.Empty;
    }

    public sealed class SendChatMessageRequest
    {
        public string Text { get; init; } = string.Empty;
    }
}
