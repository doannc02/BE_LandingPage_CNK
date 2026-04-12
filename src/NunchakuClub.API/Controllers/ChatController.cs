using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Chat.Commands;
using NunchakuClub.Application.Features.Chat.DTOs;
using NunchakuClub.Application.Features.Chat.Queries;
using NunchakuClub.Infrastructure.Services.AI;

namespace NunchakuClub.API.Controllers;

/// <summary>
/// Chat API cho người dùng cuối (cả authenticated và anonymous).
/// </summary>
[ApiController]
[Route("api/v1/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IAiChatService _aiService;
    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _logger;
    private readonly GeminiSettings _settings;

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    public ChatController(
        IAiChatService aiService,
        IMediator mediator,
        IOptions<GeminiSettings> settings,
        ILogger<ChatController> logger)
    {
        _aiService = aiService;
        _mediator = mediator;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// POST /api/v1/chat/stream
    ///
    /// Streams an AI-generated reply as Server-Sent Events (SSE).
    /// Chỉ dùng cho chat bot thuần túy (không có routing logic, không lưu history).
    /// Dùng POST /api/v1/chat/message nếu cần routing + lưu lịch sử.
    ///
    /// Each chunk: <c>data: {"content":"..."}\n\n</c>
    /// End marker: <c>data: [DONE]\n\n</c>
    /// </summary>
    [HttpPost("stream")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task Stream([FromBody] ChatRequestDto request, CancellationToken ct)
    {
        if (!IsAuthorized())
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(new { error = "Unauthorized: API key không hợp lệ." }, ct);
            return;
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Dữ liệu đầu vào không hợp lệ." }, ct);
            return;
        }

        var trimmedMessage = request.Message.Trim();
        if (string.IsNullOrEmpty(trimmedMessage))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Trường 'message' không được rỗng." }, ct);
            return;
        }

        var invalidRole = request.History.FirstOrDefault(h => h.Role is not ("user" or "assistant"));
        if (invalidRole is not null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new { error = $"history[].role chỉ nhận 'user' hoặc 'assistant', nhận được: '{invalidRole.Role}'." }, ct);
            return;
        }

        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache, no-transform";
        Response.Headers["X-Accel-Buffering"] = "no";

        var domainRequest = new ChatStreamRequest(
            trimmedMessage,
            request.History.Select(h => new ChatHistoryItem(h.Role, h.Content)).ToList());

        try
        {
            await foreach (var token in _aiService.StreamAsync(domainRequest, ct))
            {
                var json = JsonSerializer.Serialize(new { content = token }, JsonOpts);
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Chat stream cancelled by client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during chat stream.");
            try
            {
                var errorJson = JsonSerializer.Serialize(
                    new { error = "Xin lỗi, đã xảy ra lỗi kết nối. Vui lòng thử lại sau hoặc gọi hotline để được hỗ trợ." },
                    JsonOpts);
                await Response.WriteAsync($"data: {errorJson}\n\n", ct);
                await Response.WriteAsync("data: [DONE]\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
            catch
            {
                // Connection may already be gone.
            }
        }
    }

    /// <summary>
    /// POST /api/v1/chat/message
    ///
    /// Non-streaming endpoint với RAG + fallback routing. Lịch sử được lưu vào PostgreSQL.
    /// Trả về một trong 3 loại response:
    ///   - type: "AI"          → Bot đã trả lời, xem trường answer
    ///   - type: "HumanOnline" → Admin online, xem chatRoomId để subscribe Firebase realtime
    ///   - type: "LeftMessage" → Không có admin, message đã lưu, admin sẽ reply sau
    /// </summary>
    [HttpPost("message")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<ProcessChatResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<ProcessChatResponseDto>>> ProcessMessage(
        [FromBody] ChatMessageRequestDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result<ProcessChatResponseDto>.Failure("Dữ liệu đầu vào không hợp lệ."));

        var trimmedMessage = request.Message.Trim();
        if (string.IsNullOrEmpty(trimmedMessage))
            return BadRequest(Result<ProcessChatResponseDto>.Failure("Trường 'message' không được rỗng."));

        var invalidRole = request.History.FirstOrDefault(h => h.Role is not ("user" or "assistant"));
        if (invalidRole is not null)
            return BadRequest(Result<ProcessChatResponseDto>.Failure(
                $"history[].role chỉ nhận 'user' hoặc 'assistant', nhận được: '{invalidRole.Role}'."));

        // Lấy userId từ JWT nếu user đã đăng nhập (null = anonymous)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var command = new ProcessChatCommand(
            SessionId: request.SessionId,
            UserMessage: trimmedMessage,
            History: request.History.Select(h => new ChatHistoryItem(h.Role, h.Content)).ToList(),
            UserId: userId);

        try
        {
            var result = await _mediator.Send(command, ct);
            return Ok(Result<ProcessChatResponseDto>.Success(ProcessChatResponseDto.From(result)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessMessage failed for session {Session}", request.SessionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<ProcessChatResponseDto>.Failure("Đã xảy ra lỗi xử lý. Vui lòng thử lại sau."));
        }
    }

    /// <summary>
    /// POST /api/v1/chat/request-human
    ///
    /// Yêu cầu gặp nhân viên hỗ trợ trực tiếp ngay từ đầu(chuyển thẳng đến Least-Loaded Admin), bỏ qua AI.
    /// </summary>
    [HttpPost("request-human")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<ProcessChatResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<ProcessChatResponseDto>>> RequestHuman(
        [FromBody] ChatMessageRequestDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result<ProcessChatResponseDto>.Failure("Dữ liệu đầu vào không hợp lệ."));

        var trimmedMessage = string.IsNullOrWhiteSpace(request.Message) 
                           ? "Tôi muốn gặp nhân viên hỗ trợ." 
                           : request.Message.Trim();

        // Lấy userId từ JWT nếu user đã đăng nhập (null = anonymous)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var command = new ProcessChatCommand(
            SessionId: request.SessionId,
            UserMessage: trimmedMessage,
            History: request.History?.Select(h => new ChatHistoryItem(h.Role, h.Content)).ToList() ?? new List<ChatHistoryItem>(),
            UserId: userId,
            ForceHuman: true);

        try
        {
            var result = await _mediator.Send(command, ct);
            return Ok(Result<ProcessChatResponseDto>.Success(ProcessChatResponseDto.From(result)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RequestHuman failed for session {Session}", request.SessionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<ProcessChatResponseDto>.Failure("Đã xảy ra lỗi xử lý. Vui lòng thử lại sau."));
        }
    }

    /// <summary>
    /// GET /api/v1/chat/history?sessionId={sessionId}
    ///
    /// Lấy lịch sử hội thoại của một session (bot + admin messages).
    /// Không yêu cầu auth — sessionId là khóa truy cập.
    /// Trả về session gần nhất (active hoặc closed) cho sessionId đó.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<ChatSessionDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<ChatSessionDto>>> GetHistory(
        [FromQuery] string sessionId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(Result<ChatSessionDto>.Failure("sessionId không được rỗng."));

        var result = await _mediator.Send(new GetChatHistoryQuery(sessionId), ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/chat/notifications?sessionId={sessionId}
    ///
    /// Trả về reply của admin cho tin nhắn đang chờ (LeftMessage mode).
    /// Dùng cho frontend polling — trả về null nếu chưa có reply.
    /// </summary>
    [HttpGet("notifications")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<ChatNotificationDto?>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<ChatNotificationDto?>>> GetNotification(
        [FromQuery] string sessionId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(Result<ChatNotificationDto?>.Failure("sessionId is required"));

        var result = await _mediator.Send(new GetChatNotificationQuery(sessionId), ct);
        return Ok(result);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private bool IsAuthorized()
    {
        if (string.IsNullOrWhiteSpace(_settings.BackendApiKey))
            return true;

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var incomingKey = authHeader["Bearer ".Length..].Trim();
        return string.Equals(incomingKey, _settings.BackendApiKey, StringComparison.Ordinal);
    }
}
