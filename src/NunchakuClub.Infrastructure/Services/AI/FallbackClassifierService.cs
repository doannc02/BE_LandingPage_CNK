#pragma warning disable SKEXP0070  // Google AI connector is experimental in SK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
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
/// Implements IFallbackClassifierService using Semantic Kernel + Gemini.
///
/// Single non-streaming call that simultaneously:
///   1. Retrieves KB context via pgvector (RAG)
///   2. Generates a Vietnamese answer
///   3. Returns a confidence score + routing decision
///
/// Uses Gemini JSON mode (ResponseMimeType = application/json) for reliable structured output.
/// </summary>
public sealed class FallbackClassifierService : IFallbackClassifierService
{
    private readonly IChatCompletionService _chat;
    private readonly IKnowledgeBaseService _kb;
    private readonly ILogger<FallbackClassifierService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Safe default when classification fails or confidence is too low.</summary>
    private static readonly FallbackDecision HumanFallback = new(
        Confidence: 0.0f,
        NeedsHuman: true,
        Category: "other",
        Answer: "Xin lỗi, tôi chưa có thông tin về vấn đề này. Bạn vui lòng liên hệ hotline 0868.699.860 hoặc nhắn tin Fanpage để được hỗ trợ nhanh nhất nhé!"
    );

    public FallbackClassifierService(
        IOptions<GeminiSettings> opts,
        IKnowledgeBaseService kb,
        ILogger<FallbackClassifierService> logger)
    {
        _kb = kb;
        _logger = logger;

        var settings = opts.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("GeminiSettings:ApiKey is not configured.");

        var kernel = Kernel.CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(
                modelId: settings.ModelId,
                apiKey: settings.ApiKey)
            .Build();

        _chat = kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<FallbackDecision> ClassifyAsync(
        string userMessage,
        IReadOnlyList<ChatHistoryItem> history,
        CancellationToken ct = default)
    {
        // 1. Retrieve KB context (top-3 chunks)
        IReadOnlyList<KnowledgeChunk> chunks;
        try
        {
            chunks = await _kb.SearchAsync(userMessage, topK: 3, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "KB search failed in classifier — using empty context");
            chunks = [];
        }

        var context = chunks.Count > 0
            ? string.Join("\n\n---\n\n", chunks.Select(c => c.Content))
            : "(Không có thông tin liên quan trong kho kiến thức)";

        // 2. Build chat history (last 3 turns for context awareness)
        var chatHistory = new ChatHistory(BuildSystemPrompt(context));

        var recentHistory = history
            .Where(h => !string.IsNullOrWhiteSpace(h.Content))
            .TakeLast(6)
            .ToList();

        foreach (var item in recentHistory)
        {
            if (item.Role == "user") chatHistory.AddUserMessage(item.Content);
            else if (item.Role == "assistant") chatHistory.AddAssistantMessage(item.Content);
        }

        chatHistory.AddUserMessage(userMessage);

        // 3. Call Gemini non-streaming, temperature thấp để output ổn định
        // JSON mode không available trong SK 1.21.1 — dùng prompt engineering + robust parsing
        var executionSettings = new GeminiPromptExecutionSettings
        {
            MaxTokens = 600,
            Temperature = 0.1f,
        };

        try
        {
            var response = await _chat.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: executionSettings,
                cancellationToken: ct);

            var rawJson = response.Content ?? string.Empty;
            _logger.LogDebug("Classifier response: {Json}", rawJson);

            return ParseDecision(rawJson) ?? HumanFallback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Classifier Gemini call failed — defaulting to NeedsHuman");
            return HumanFallback;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private FallbackDecision? ParseDecision(string json)
    {
        try
        {
            // Strip markdown code fences if Gemini wraps the JSON
            var cleaned = json.Trim();
            if (cleaned.StartsWith("```", StringComparison.Ordinal))
            {
                var start = cleaned.IndexOf('{');
                var end = cleaned.LastIndexOf('}');
                if (start >= 0 && end > start)
                    cleaned = cleaned[start..(end + 1)];
            }

            var raw = JsonSerializer.Deserialize<ClassifierRawResponse>(cleaned, JsonOpts);
            if (raw is null) return null;

            if (string.IsNullOrWhiteSpace(raw.Answer))
            {
                _logger.LogWarning("Classifier returned empty answer — treating as HumanFallback");
                return null;
            }

            return new FallbackDecision(
                Confidence: Math.Clamp(raw.Confidence, 0f, 1f),
                NeedsHuman: raw.NeedsHuman,
                Category: string.IsNullOrWhiteSpace(raw.Category) ? "other" : raw.Category,
                Answer: raw.Answer
            );
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON parse failed for classifier response");
            return null;
        }
    }

    // $$""" (double-dollar raw string): {{expr}} = interpolation, bare { } = literal
    private static string BuildSystemPrompt(string context) => $$"""
        Bạn là trợ lý AI của Võ đường Côn Nhị Khúc Hà Đông — võ đường dạy môn Côn Nhị Khúc tại Hà Nội.

        === THÔNG TIN VÕ ĐƯỜNG (từ knowledge base) ===
        {{context}}
        ===============================================

        NHIỆM VỤ: Phân tích câu hỏi của user, soạn câu trả lời tiếng Việt, và đánh giá độ tự tin.

        QUY TẮC confidence:
        - 0.85 – 1.0 : Có thông tin chính xác trong context, trả lời chắc chắn
        - 0.65 – 0.85: Có thông tin một phần, suy luận hợp lý từ context
        - 0.40 – 0.65: Thông tin mờ nhạt hoặc cần xác nhận thêm
        - 0.00 – 0.40: Không có thông tin liên quan

        QUY TẮC needs_human = true:
        - Câu hỏi hoàn toàn ngoài phạm vi võ đường / môn võ
        - Khiếu nại, tranh chấp, vấn đề nhạy cảm
        - Hỏi thông tin cá nhân cụ thể của người khác
        - Yêu cầu tư vấn phức tạp cần can thiệp trực tiếp
        - Câu hỏi không thể trả lời từ knowledge base (confidence < 0.5)

        Các category hợp lệ: address, fee, schedule, registration, online, training, discipline, legal, achievement, complaint, other

        Trả về JSON hợp lệ DUY NHẤT (không kèm markdown, không giải thích):
        {
          "confidence": <số thực 0.0-1.0>,
          "needs_human": <true/false>,
          "category": "<category>",
          "answer": "<câu trả lời tiếng Việt thân thiện, ngắn gọn>"
        }
        """;

    // ── JSON deserialization model ─────────────────────────────────────────────

    private sealed class ClassifierRawResponse
    {
        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }

        [JsonPropertyName("needs_human")]
        public bool NeedsHuman { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("answer")]
        public string? Answer { get; set; }
    }
}
