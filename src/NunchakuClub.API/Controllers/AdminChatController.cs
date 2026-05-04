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
    private readonly IApplicationDbContext _db;
    private readonly IFirebaseChatService _firebaseChat;

    public AdminChatController(
        IApplicationDbContext db,
        IFirebaseChatService firebaseChat)
    {
        _db = db;
        _firebaseChat = firebaseChat;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string AdminId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── FCM token management ──────────────────────────────────────────────────

    /// <summary>
    /// POST /api/admin/fcm-token
    ///
    /// Admin gửi FCM device token sau khi đăng nhập + browser grant notification permission.
    /// Token được lưu vào bảng user_fcm_tokens (một user có thể có nhiều token — nhiều thiết bị).
    /// Nếu token đã tồn tại cho user này, bỏ qua (không tạo bản ghi trùng).
    /// Gọi mỗi lần: đăng nhập, hoặc FCM token bị refresh.
    /// </summary>
    [HttpPost("fcm-token")]
    public async Task<ActionResult<Result<bool>>> SaveFcmToken(
        [FromBody] SaveFcmTokenDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(Result<bool>.Failure("FCM token không được rỗng."));

        if (!Guid.TryParse(AdminId, out var adminGuid))
            return Unauthorized(Result<bool>.Failure("Không xác định được admin ID."));

        var token = dto.Token.Trim();

        // Kiểm tra token đã tồn tại chưa để tránh trùng lặp
        var exists = await _db.UserFcmTokens
            .AnyAsync(t => t.UserId == adminGuid && t.Token == token, ct);

        if (!exists)
        {
            _db.UserFcmTokens.Add(new NunchakuClub.Domain.Entities.UserFcmToken
            {
                UserId = adminGuid,
                Token = token,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        return Ok(Result<bool>.Success(true));
    }

    // ── Pending messages (luồng A — admin offline) ───────────────────────────

    /// <summary>
    /// GET /api/admin/pending-messages?status=Pending&amp;page=1&amp;pageSize=20
    ///
    /// Lấy danh sách tin nhắn pending (user gửi khi không có admin online).
    /// Mặc định trả về Pending + Assigned (chưa được giải quyết).
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
    ///
    /// Admin reply một pending message. Backend:
    ///   1. Cập nhật PostgreSQL (status → Replied)
    ///   2. Lưu ConversationMessage với Role = Admin
    ///   3. Ghi notification vào Firebase /notifications/{sessionId}
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

        message.Status = PendingMessageStatus.Replied;
        message.AdminReply = dto.Text.Trim();
        message.AssignedAdminId = AdminId;
        message.RepliedAt = DateTime.UtcNow;

        // Lưu admin message vào conversation history
        await AppendAdminMessageToSessionAsync(
            sessionId: message.SessionId,
            pendingMessageId: id,
            content: dto.Text.Trim(),
            ct: ct);

        await _db.SaveChangesAsync(ct);

        // Push reply notification to Firebase so user sees it if still online
        await _firebaseChat.NotifyUserReplyAsync(message.SessionId, message.AdminReply, AdminId, ct);

        return Ok(Result<bool>.Success(true));
    }

    // ── Notification badge ────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/admin/pending-count
    ///
    /// Số lượng pending messages chưa được xử lý (Pending + Assigned).
    /// Dùng để hiển thị badge đỏ trên notification icon.
    /// Poll mỗi 30–60 giây.
    /// </summary>
    [HttpGet("pending-count")]
    public async Task<ActionResult<Result<PendingCountDto>>> GetPendingCount(CancellationToken ct)
    {
        var count = await _db.PendingUserMessages
            .CountAsync(m => m.Status == PendingMessageStatus.Pending
                          || m.Status == PendingMessageStatus.Assigned, ct);

        return Ok(Result<PendingCountDto>.Success(new PendingCountDto(count)));
    }

    /// <summary>
    /// POST /api/admin/pending-messages/{id}/close
    ///
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

        // Đóng session liên quan
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(
                s => s.PendingMessageId == id && s.Status != ChatSessionStatus.Closed, ct);

        if (session is not null)
        {
            session.Status = ChatSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(Result<bool>.Success(true));
    }

    // ── Active Firebase chat rooms (luồng B — admin online) ──────────────────

    /// <summary>
    /// POST /api/admin/chats/{chatId}/message
    ///
    /// Admin gửi message vào một chat room đang active trong Firebase.
    /// Message đồng thời được persist vào PostgreSQL (conversation_messages).
    /// Dùng khi admin muốn gửi qua backend thay vì Firebase SDK trực tiếp.
    /// </summary>
    [HttpPost("chats/{chatId}/message")]
    public async Task<ActionResult<Result<bool>>> SendChatMessage(
        string chatId,
        [FromBody] SendChatMessageDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(Result<bool>.Failure("Nội dung message không được rỗng."));

        // Ghi vào Firebase Realtime Database
        await _firebaseChat.SendMessageAsync(chatId, $"admin:{AdminId}", dto.Text.Trim(), ct);

        // Persist vào PostgreSQL
        await AppendAdminMessageToSessionAsync(
            firebaseChatRoomId: chatId,
            content: dto.Text.Trim(),
            ct: ct);

        await _db.SaveChangesAsync(ct);

        return Ok(Result<bool>.Success(true));
    }

    /// <summary>
    /// POST /api/admin/chats/{chatId}/close
    ///
    /// Đóng một chat room trong Firebase (status → "closed").
    /// Cập nhật trạng thái ChatSession → Closed trong PostgreSQL.
    /// </summary>
    [HttpPost("chats/{chatId}/close")]
    public async Task<ActionResult<Result<bool>>> CloseChat(
        string chatId,
        CancellationToken ct)
    {
        await _firebaseChat.CloseChatAsync(chatId, ct);

        // Đóng session trong PostgreSQL
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(
                s => s.FirebaseChatRoomId == chatId && s.Status != ChatSessionStatus.Closed, ct);

        if (session is not null)
        {
            session.Status = ChatSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(Result<bool>.Success(true));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Tìm session theo FirebaseChatRoomId và thêm admin message.
    /// </summary>
    private async Task AppendAdminMessageToSessionAsync(
        string? firebaseChatRoomId = null,
        Guid? pendingMessageId = null,
        string? sessionId = null,
        string content = "",
        CancellationToken ct = default)
    {
        ChatSession? session = null;

        if (firebaseChatRoomId is not null)
        {
            session = await _db.ChatSessions
                .FirstOrDefaultAsync(s => s.FirebaseChatRoomId == firebaseChatRoomId, ct);
        }
        else if (pendingMessageId.HasValue)
        {
            session = await _db.ChatSessions
                .FirstOrDefaultAsync(s => s.PendingMessageId == pendingMessageId, ct);
        }
        else if (sessionId is not null)
        {
            session = await _db.ChatSessions
                .Where(s => s.SessionId == sessionId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        if (session is null) return;

        _db.ConversationMessages.Add(new ConversationMessage
        {
            ChatSessionId = session.Id,
            SessionId = session.SessionId,
            Role = ConversationMessageRole.Admin,
            Content = content,
            SenderAdminId = AdminId
        });
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
