using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Features.Chat.DTOs;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Chat.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

public record ProcessChatCommand(
    string SessionId,
    string UserMessage,
    IReadOnlyList<ChatHistoryItem> History,
    string? UserId = null
) : IRequest<ProcessChatResult>;

// ── Result ────────────────────────────────────────────────────────────────────

public sealed record ProcessChatResult(
    ChatResponseType ResponseType,
    /// <summary>Set when ResponseType == AI.</summary>
    string? AiAnswer,
    /// <summary>Firebase chat room ID. Set when ResponseType == HumanOnline.</summary>
    string? ChatRoomId,
    /// <summary>PendingUserMessage GUID. Set when ResponseType == LeftMessage.</summary>
    string? MessageId
);

public enum ChatResponseType
{
    /// <summary>AI answered with sufficient confidence.</summary>
    AI,
    /// <summary>Admin is online — realtime chat room created in Firebase.</summary>
    HumanOnline,
    /// <summary>No admin online — message saved, push notification sent.</summary>
    LeftMessage
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ProcessChatHandler : IRequestHandler<ProcessChatCommand, ProcessChatResult>
{
    /// <summary>Confidence must meet or exceed this threshold for AI to answer directly.</summary>
    private const float ConfidenceThreshold = 0.75f;

    private readonly IFallbackClassifierService _classifier;
    private readonly IFirebasePresenceService _presence;
    private readonly IFirebaseChatService _firebaseChat;
    private readonly IFcmNotificationService _fcm;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<ProcessChatHandler> _logger;

    public ProcessChatHandler(
        IFallbackClassifierService classifier,
        IFirebasePresenceService presence,
        IFirebaseChatService firebaseChat,
        IFcmNotificationService fcm,
        IApplicationDbContext db,
        ILogger<ProcessChatHandler> logger)
    {
        _classifier = classifier;
        _presence = presence;
        _firebaseChat = firebaseChat;
        _fcm = fcm;
        _db = db;
        _logger = logger;
    }

    public async Task<ProcessChatResult> Handle(ProcessChatCommand request, CancellationToken ct)
    {
        // 1. RAG + confidence scoring (single Gemini call)
        var decision = await _classifier.ClassifyAsync(request.UserMessage, request.History, ct);

        _logger.LogInformation(
            "Chat classify: Session={Session} Confidence={Conf:F2} NeedsHuman={Human} Category={Cat}",
            request.SessionId, decision.Confidence, decision.NeedsHuman, decision.Category);

        // 2. AI is confident enough → lưu lịch sử, trả về answer
        if (!decision.NeedsHuman && decision.Confidence >= ConfidenceThreshold)
        {
            var session = await GetOrCreateSessionAsync(request.SessionId, request.UserId, ct);
            AddUserMessage(session, request.UserMessage);
            AddBotMessage(session, decision.Answer);
            await SaveWithRetryAsync(ct);

            return new ProcessChatResult(ChatResponseType.AI, decision.Answer, null, null);
        }

        // 3. Fallback: check for online admin
        var onlineAdmin = await _presence.GetFirstOnlineAdminAsync(ct);

        if (onlineAdmin is not null)
        {
            _logger.LogInformation(
                "Admin {AdminId} online — creating Firebase chat room for session {Session}",
                onlineAdmin.AdminId, request.SessionId);

            var chatRoomId = await _firebaseChat.CreateChatRoomAsync(
                request.SessionId,
                onlineAdmin.AdminId,
                request.UserMessage,
                ct);

            // Lưu session + tin nhắn đầu tiên của user
            var session = await GetOrCreateSessionAsync(request.SessionId, request.UserId, ct);
            session.Status = ChatSessionStatus.HumanHandoff;
            session.HandoffType = ChatHandoffType.Firebase;
            session.FirebaseChatRoomId = chatRoomId;
            AddUserMessage(session, request.UserMessage);
            await SaveWithRetryAsync(ct);

            return new ProcessChatResult(ChatResponseType.HumanOnline, null, chatRoomId, null);
        }

        // 4. No admin online → lưu pending message + push notification
        _logger.LogInformation(
            "No admin online — saving pending message for session {Session}", request.SessionId);

        var pending = new PendingUserMessage
        {
            SessionId = request.SessionId,
            UserMessage = request.UserMessage,
            UserId = request.UserId,
            NextNotificationAt = DateTime.UtcNow
        };
        _db.PendingUserMessages.Add(pending);

        // BaseEntity.Id = Guid.NewGuid() nên pending.Id đã có ngay, không cần 2 lần SaveChanges
        var offlineSession = await GetOrCreateSessionAsync(request.SessionId, request.UserId, ct);
        offlineSession.Status = ChatSessionStatus.HumanHandoff;
        offlineSession.HandoffType = ChatHandoffType.Pending;
        offlineSession.PendingMessageId = pending.Id;
        AddUserMessage(offlineSession, request.UserMessage);

        await SaveWithRetryAsync(ct);

        await _fcm.NotifyAllAdminsAsync(request.UserMessage, pending.Id, ct);

        return new ProcessChatResult(ChatResponseType.LeftMessage, null, null, pending.Id.ToString());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về session đang active (Status != Closed) cho sessionId.
    /// Nếu chưa có hoặc tất cả đã Closed → tạo mới.
    /// </summary>
    private async Task<ChatSession> GetOrCreateSessionAsync(
        string sessionId, string? userId, CancellationToken ct)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(
                s => s.SessionId == sessionId && s.Status != ChatSessionStatus.Closed,
                ct);

        if (session is not null)
            return session;

        session = new ChatSession
        {
            SessionId = sessionId,
            UserId = Guid.TryParse(userId, out var uid) ? uid : null
        };
        _db.ChatSessions.Add(session);
        return session;
    }

    /// <summary>
    /// Thêm ConversationMessage trực tiếp qua DbSet với FK explicit.
    /// Tránh bug EF Core DetectChanges khi session chưa được Include Messages.
    /// </summary>
    private void AddUserMessage(ChatSession session, string content)
    {
        _db.ConversationMessages.Add(new ConversationMessage
        {
            ChatSessionId = session.Id,
            SessionId = session.SessionId,
            Role = ConversationMessageRole.User,
            Content = content
        });
    }

    private void AddBotMessage(ChatSession session, string content)
    {
        if (string.IsNullOrEmpty(content)) return;

        _db.ConversationMessages.Add(new ConversationMessage
        {
            ChatSessionId = session.Id,
            SessionId = session.SessionId,
            Role = ConversationMessageRole.Bot,
            Content = content
        });
    }

    /// <summary>
    /// Lưu changes, tự động reload và retry một lần nếu bị concurrency conflict.
    /// </summary>
    private async Task SaveWithRetryAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict on SaveChanges — reloading and retrying once");
            foreach (var entry in ex.Entries)
                await entry.ReloadAsync(ct);
            await _db.SaveChangesAsync(ct);
        }
    }
}
