using System;
using System.Linq;
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
using NunchakuClub.Infrastructure.Services.AI;

namespace NunchakuClub.API.Controllers;

/// <summary>
/// POST /v1/chat/stream
///
/// Accepts a chat message + history and streams a Server-Sent Events (SSE) response
/// powered by Gemini 1.5 Flash via Semantic Kernel + RAG.
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
    /// Streams an AI-generated reply as Server-Sent Events (SSE).
    ///
    /// Each chunk: <c>data: {"content":"..."}\n\n</c>
    /// End marker: <c>data: [DONE]\n\n</c>
    /// </summary>
    [HttpPost("stream")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task Stream(
        [FromBody] ChatRequestDto request,
        CancellationToken ct)
    {
        // --- 1. Optional server-to-server API key validation ---
        if (!IsAuthorized())
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(
                new { error = "Unauthorized: API key không hợp lệ." }, ct);
            return;
        }

        // --- 2. Input validation (DataAnnotations already ran via ModelState) ---
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new { error = "Dữ liệu đầu vào không hợp lệ." }, ct);
            return;
        }

        var trimmedMessage = request.Message.Trim();
        if (string.IsNullOrEmpty(trimmedMessage))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new { error = "Trường 'message' không được rỗng." }, ct);
            return;
        }

        // Validate history roles
        var invalidRole = request.History
            .FirstOrDefault(h => h.Role is not ("user" or "assistant"));
        if (invalidRole is not null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new { error = $"history[].role chỉ nhận 'user' hoặc 'assistant', nhận được: '{invalidRole.Role}'." },
                ct);
            return;
        }

        // --- 3. Set SSE headers before writing body ---
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache, no-transform";
        Response.Headers["X-Accel-Buffering"] = "no";

        // --- 4. Map DTO → domain model ---
        var domainRequest = new ChatStreamRequest(
            trimmedMessage,
            request.History
                .Select(h => new ChatHistoryItem(h.Role, h.Content))
                .ToList()
        );

        // --- 5. Stream ---
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
            // Client disconnected — normal; no action needed.
            _logger.LogDebug("Chat stream cancelled by client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during chat stream.");

            // Headers already sent — cannot change status code.
            // Signal error via a final SSE chunk so the frontend can handle it.
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
                // Swallow — connection may already be gone.
            }
        }
    }

    /// <summary>
    /// POST /api/v1/chat/message
    ///
    /// Non-streaming endpoint với RAG + fallback logic.
    /// Trả về một trong 3 loại response:
    ///   - type: "AI"          → AI đã trả lời, xem trường answer
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

        var invalidRole = request.History
            .FirstOrDefault(h => h.Role is not ("user" or "assistant"));
        if (invalidRole is not null)
            return BadRequest(Result<ProcessChatResponseDto>.Failure(
                $"history[].role chỉ nhận 'user' hoặc 'assistant', nhận được: '{invalidRole.Role}'."));

        var command = new ProcessChatCommand(
            SessionId: request.SessionId,
            UserMessage: trimmedMessage,
            History: request.History
                .Select(h => new ChatHistoryItem(h.Role, h.Content))
                .ToList()
        );

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

    // ---------------------------------------------------------------------------

    private bool IsAuthorized()
    {
        // If BackendApiKey is not configured, skip auth check entirely.
        if (string.IsNullOrWhiteSpace(_settings.BackendApiKey))
            return true;

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var incomingKey = authHeader["Bearer ".Length..].Trim();
        return string.Equals(incomingKey, _settings.BackendApiKey, StringComparison.Ordinal);
    }
}
