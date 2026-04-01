using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.API.Controllers;

/// <summary>
/// Endpoints dành riêng cho Admin/Editor để quản lý chat support.
///
/// Luồng:
///   1. User gửi message → AI không đủ confidence → Admin offline
///   2. Message lưu vào pending_user_messages (PostgreSQL)
///   3. Admin vào dashboard → GET /pending-messages → reply
///
///   Nếu Admin online tại thời điểm user gửi:
///   → Firebase chat room được tạo, admin thấy qua Firebase subscription.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,Editor")]
public sealed class AdminChatController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IFirebaseChatService _firebaseChat;

    public AdminChatController(
        IApplicationDbContext db,
        IFirebaseChatService firebaseChat)
    {
        _db = db;
        _firebaseChat = firebaseChat;
    }

    // ── FCM token management ──────────────────────────────────────────────────

    /// <summary>
    /// POST /api/admin/fcm-token
    ///
    /// Admin gửi FCM device token sau khi đăng nhập + browser grant notification permission.
    /// Token được lưu vào PostgreSQL (users.fcm_token) để dùng làm fallback notification
    /// khi Firebase presence không có token (admin offline).
    ///
    /// Frontend gọi endpoint này mỗi lần:
    ///   - Admin login
    ///   - Token refresh (FCM token thay đổi)
    /// </summary>
    [HttpPost("fcm-token")]
    public async Task<ActionResult<Result<bool>>> SaveFcmToken(
        [FromBody] SaveFcmTokenDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(Result<bool>.Failure("FCM token không được rỗng."));

        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdStr, out var adminId))
            return Unauthorized(Result<bool>.Failure("Không xác định được admin ID."));

        var user = await _db.Users.FindAsync([adminId], ct);
        if (user is null)
            return NotFound(Result<bool>.Failure("Không tìm thấy tài khoản."));

        user.FcmToken = dto.Token.Trim();
        await _db.SaveChangesAsync(ct);

        return Ok(Result<bool>.Success(true));
    }

    // ── Pending messages (admin offline) ─────────────────────────────────────

    /// <summary>
    /// GET /api/admin/pending-messages
    /// Lấy danh sách tin nhắn pending (user gửi khi không có admin online).
    /// Hỗ trợ lọc theo status.
    /// </summary>
    [HttpGet("pending-messages")]
    public async Task<ActionResult<Result<List<PendingMessageDto>>>> GetPendingMessages(
        [FromQuery] PendingMessageStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = _db.PendingUserMessages.AsQueryable();

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);
        else
            query = query.Where(m => m.Status == PendingMessageStatus.Pending
                                  || m.Status == PendingMessageStatus.Assigned);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new PendingMessageDto(
                m.Id,
                m.SessionId,
                m.UserMessage,
                m.Status.ToString(),
                m.AdminReply,
                m.AssignedAdminId,
                m.CreatedAt,
                m.RepliedAt))
            .ToListAsync(ct);

        return Ok(Result<List<PendingMessageDto>>.Success(items));
    }

    /// <summary>
    /// POST /api/admin/pending-messages/{id}/reply
    /// Admin reply một pending message. Backend:
    ///   1. Cập nhật PostgreSQL (status → Replied)
    ///   2. Ghi notification vào Firebase /notifications/{sessionId}
    ///      → nếu user vẫn còn mở tab, sẽ thấy reply ngay lập tức
    /// </summary>
    [HttpPost("pending-messages/{id:guid}/reply")]
    public async Task<ActionResult<Result<bool>>> ReplyPendingMessage(
        Guid id,
        [FromBody] ReplyPendingMessageDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(Result<bool>.Failure("Nội dung reply không được rỗng."));

        var message = await _db.PendingUserMessages.FindAsync([id], ct);
        if (message is null)
            return NotFound(Result<bool>.Failure($"Không tìm thấy pending message {id}."));

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        message.Status = PendingMessageStatus.Replied;
        message.AdminReply = dto.Text.Trim();
        message.AssignedAdminId = adminId;
        message.RepliedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Push reply notification to Firebase so user sees it if still online
        await _firebaseChat.NotifyUserReplyAsync(message.SessionId, message.AdminReply, adminId, ct);

        return Ok(Result<bool>.Success(true));
    }

    // ── Notification badge ────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/admin/pending-count
    /// Trả về số lượng pending messages chưa được xử lý.
    /// Frontend dùng để hiển thị badge đỏ trên notification icon.
    /// Poll mỗi 30–60 giây hoặc dùng SSE/WebSocket cho realtime.
    /// </summary>
    [HttpGet("pending-count")]
    public async Task<ActionResult<Result<PendingCountDto>>> GetPendingCount(CancellationToken ct)
    {
        var count = await _db.PendingUserMessages
            .CountAsync(m => m.Status == PendingMessageStatus.Pending
                          || m.Status == PendingMessageStatus.Assigned, ct);

        return Ok(Result<PendingCountDto>.Success(new PendingCountDto(count)));
    }

    // ── Mark read / close ─────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/admin/pending-messages/{id}/close
    /// Đóng một pending message (admin không reply, đánh dấu đã xem / bỏ qua).
    /// Status → Closed. Không gửi notification cho user.
    /// </summary>
    [HttpPost("pending-messages/{id:guid}/close")]
    public async Task<ActionResult<Result<bool>>> ClosePendingMessage(
        Guid id,
        CancellationToken ct)
    {
        var message = await _db.PendingUserMessages.FindAsync([id], ct);
        if (message is null)
            return NotFound(Result<bool>.Failure($"Không tìm thấy pending message {id}."));

        message.Status = PendingMessageStatus.Closed;
        await _db.SaveChangesAsync(ct);

        return Ok(Result<bool>.Success(true));
    }

    // ── Active Firebase chat rooms ────────────────────────────────────────────

    /// <summary>
    /// POST /api/admin/chats/{chatId}/message
    /// Admin gửi message vào một chat room đang active trong Firebase.
    /// Dùng khi admin muốn gửi message qua backend thay vì Firebase SDK trực tiếp.
    /// </summary>
    [HttpPost("chats/{chatId}/message")]
    public async Task<ActionResult<Result<bool>>> SendChatMessage(
        string chatId,
        [FromBody] SendChatMessageDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(Result<bool>.Failure("Nội dung message không được rỗng."));

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";

        await _firebaseChat.SendMessageAsync(chatId, $"admin:{adminId}", dto.Text.Trim(), ct);

        return Ok(Result<bool>.Success(true));
    }

    /// <summary>
    /// POST /api/admin/chats/{chatId}/close
    /// Đóng một chat room trong Firebase (status → "closed").
    /// </summary>
    [HttpPost("chats/{chatId}/close")]
    public async Task<ActionResult<Result<bool>>> CloseChat(
        string chatId,
        CancellationToken ct)
    {
        await _firebaseChat.CloseChatAsync(chatId, ct);
        return Ok(Result<bool>.Success(true));
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record PendingMessageDto(
    Guid Id,
    string SessionId,
    string UserMessage,
    string Status,
    string? AdminReply,
    string? AssignedAdminId,
    DateTime CreatedAt,
    DateTime? RepliedAt
);

public sealed class ReplyPendingMessageDto
{
    public string Text { get; init; } = string.Empty;
}

public sealed class SendChatMessageDto
{
    public string Text { get; init; } = string.Empty;
}

public sealed class SaveFcmTokenDto
{
    public string Token { get; init; } = string.Empty;
}

public sealed record PendingCountDto(int Count);
