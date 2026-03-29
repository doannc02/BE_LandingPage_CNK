#pragma warning disable SKEXP0070  // Google AI connector is experimental in SK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Features.Chat.DTOs;

namespace NunchakuClub.Infrastructure.Services.AI;

/// <summary>
/// Implements IAiChatService using Semantic Kernel + Gemini 1.5 Flash.
///
/// RAG pipeline:
///   1. Retrieve relevant context from IKnowledgeBaseService (mock → pgvector later)
///   2. Build a system prompt that injects the context
///   3. Reconstruct chat history (capped at MaxHistoryTurns)
///   4. Stream Gemini response token-by-token via IAsyncEnumerable&lt;string&gt;
/// </summary>
public sealed class GeminiChatService : IAiChatService
{
    private readonly IChatCompletionService _chat;
    private readonly IKnowledgeBaseService _kb;
    private readonly ILogger<GeminiChatService> _logger;
    private readonly int _maxHistoryTurns;

    public GeminiChatService(
        IOptions<GeminiSettings> opts,
        IKnowledgeBaseService kb,
        ILogger<GeminiChatService> logger)
    {
        _kb = kb;
        _logger = logger;
        _maxHistoryTurns = opts.Value.MaxHistoryTurns;

        var settings = opts.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException(
                "GeminiSettings:ApiKey is not configured. " +
                "Add it to appsettings.json or environment variables.");

        var kernel = Kernel.CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(
                modelId: settings.ModelId,
                apiKey: settings.ApiKey)
            .Build();

        _chat = kernel.GetRequiredService<IChatCompletionService>();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> StreamAsync(
        ChatStreamRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1. RAG — retrieve relevant knowledge chunks
        IReadOnlyList<KnowledgeChunk> chunks;
        try
        {
            chunks = await _kb.SearchAsync(request.Message, topK: 2, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Knowledge base search failed; continuing without context.");
            chunks = [];
        }

        var context = chunks.Count > 0
            ? string.Join("\n\n---\n\n", chunks.Select(c => c.Content))
            : "Không có thông tin cụ thể.";

        // 2. Build system prompt with injected context
        var systemPrompt = BuildSystemPrompt(context);

        // 3. Reconstruct chat history (oldest-first, capped)
        var chatHistory = new ChatHistory(systemPrompt);

        var historyToSend = request.History
            .Where(h => !string.IsNullOrWhiteSpace(h.Content))  // drop incomplete streaming turns
            .TakeLast(_maxHistoryTurns * 2)   // *2 because each turn = user + assistant
            .ToList();

        foreach (var item in historyToSend)
        {
            if (item.Role == "user")
                chatHistory.AddUserMessage(item.Content);
            else if (item.Role == "assistant")
                chatHistory.AddAssistantMessage(item.Content);
        }

        chatHistory.AddUserMessage(request.Message);

        // 4. Stream tokens from Gemini (with retry on 429)
        // yield return cannot be inside a try-catch, so we use a Channel to decouple
        // the retry producer from the consumer (yield).
        _logger.LogInformation(
            "Streaming Gemini response. HistoryTurns={Turns}, ContextChunks={Chunks}",
            historyToSend.Count / 2, chunks.Count);

        var channel = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions { SingleWriter = true, SingleReader = true });

        _ = ProduceWithRetryAsync(chatHistory, channel.Writer, ct);

        await foreach (var token in channel.Reader.ReadAllAsync(ct))
            yield return token;
    }

    // ---------------------------------------------------------------------------

    private async Task ProduceWithRetryAsync(
        ChatHistory chatHistory,
        ChannelWriter<string> writer,
        CancellationToken ct)
    {
        const int maxRetries = 3;
        Exception? lastEx = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await foreach (var chunk in _chat
                    .GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: ct))
                {
                    if (!string.IsNullOrEmpty(chunk.Content))
                        await writer.WriteAsync(chunk.Content, ct);
                }
                writer.Complete();
                return;
            }
            catch (HttpOperationException ex)
                when (ex.StatusCode == HttpStatusCode.TooManyRequests && attempt < maxRetries)
            {
                lastEx = ex;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s
                _logger.LogWarning(
                    "Gemini 429 rate limit (attempt {Attempt}/{Max}), retrying in {Delay}s…",
                    attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                writer.Complete(ex);
                return;
            }
        }

        writer.Complete(lastEx);
    }

    // ---------------------------------------------------------------------------

    private static string BuildSystemPrompt(string context) => $"""
        Bạn là trợ lý AI thân thiện của Câu lạc bộ Côn Nhị Khúc Hà Đông (CLB CNK Hà Đông).
        Nhiệm vụ: hỗ trợ học viên, phụ huynh và người quan tâm giải đáp các thắc mắc về CLB.

        Quy tắc trả lời:
        - Chỉ trả lời bằng tiếng Việt, giọng điệu thân thiện và ngắn gọn.
        - Trả lời dựa trên THÔNG TIN CLB bên dưới. Không bịa thêm.
        - Nếu câu hỏi nằm ngoài thông tin được cung cấp, trả lời:
          "Xin lỗi, tôi chưa có thông tin về vấn đề này. Bạn vui lòng liên hệ hotline 0912 345 678 hoặc nhắn tin Fanpage để được hỗ trợ nhanh nhất nhé!"
        - Không thảo luận các chủ đề không liên quan đến CLB.

        ===THÔNG TIN CLB===
        {context}
        ===================
        """;
}
