using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Domain.Entities;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace NunchakuClub.Infrastructure.Services.AI;

/// <summary>
/// Production knowledge base using pgvector cosine-similarity search.
///
/// SearchAsync pipeline:
///   1. Generate query embedding via IEmbeddingService (Google text-embedding-004)
///   2. ORDER BY embedding &lt;=&gt; queryVector (cosine distance) via HNSW index
///   3. Fall back to keyword matching when DB has no embedded documents
///
/// Document management methods auto-generate embeddings on Add/Update.
/// </summary>
public sealed class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IApplicationDbContext _db;
    private readonly IEmbeddingService _embedding;
    private readonly ILogger<KnowledgeBaseService> _logger;

    public KnowledgeBaseService(
        IApplicationDbContext db,
        IEmbeddingService embedding,
        ILogger<KnowledgeBaseService> logger)
    {
        _db = db;
        _embedding = embedding;
        _logger = logger;
    }

    // ── Retrieval ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<KnowledgeChunk>> SearchAsync(
        string query, int topK = 2, CancellationToken ct = default)
    {
        try
        {
            var queryEmbedding = await _embedding.GenerateAsync(query, ct);
            var queryVector = new Vector(queryEmbedding);

            var docs = await _db.KnowledgeDocuments
                .Where(d => d.Embedding != null)
                .OrderBy(d => d.Embedding!.CosineDistance(queryVector))
                .Take(topK)
                .ToListAsync(ct);

            if (docs.Count > 0)
            {
                _logger.LogDebug("Vector search returned {Count} chunks for query: {Query}",
                    docs.Count, query);

                return docs.Select(d => new KnowledgeChunk(d.Content, d.Source, 1f)).ToList();
            }

            _logger.LogWarning("No embedded documents in DB — falling back to keyword search.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vector search failed — falling back to keyword search.");
        }

        return KeywordFallback(query, topK);
    }

    // ── Document management ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<KnowledgeDocumentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await _db.KnowledgeDocuments
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return docs.Select(ToDto).ToList();
    }

    public async Task<KnowledgeDocumentDto> AddAsync(
        string content, string source, string? title, CancellationToken ct = default)
    {
        var embedding = await GenerateEmbeddingSafe(content, ct);

        var doc = new KnowledgeDocument
        {
            Content = content,
            Source = source,
            Title = title,
            Embedding = embedding is not null ? new Vector(embedding) : null
        };

        _db.KnowledgeDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Added knowledge document {Id} (source: {Source})", doc.Id, source);
        return ToDto(doc);
    }

    public async Task<KnowledgeDocumentDto> UpdateAsync(
        Guid id, string content, string source, string? title, CancellationToken ct = default)
    {
        var doc = await _db.KnowledgeDocuments.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Document {id} not found.");

        var embedding = await GenerateEmbeddingSafe(content, ct);

        doc.Content = content;
        doc.Source = source;
        doc.Title = title;
        doc.Embedding = embedding is not null ? new Vector(embedding) : null;
        doc.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated knowledge document {Id}", id);
        return ToDto(doc);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _db.KnowledgeDocuments.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Document {id} not found.");

        _db.KnowledgeDocuments.Remove(doc);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted knowledge document {Id}", id);
    }

    public async Task SeedDefaultAsync(CancellationToken ct = default)
    {
        var existingCount = await _db.KnowledgeDocuments.CountAsync(ct);

        if (existingCount == DefaultDocuments.Length)
        {
            // Count matches — but check if any docs are missing embeddings
            var missingEmbedCount = await _db.KnowledgeDocuments
                .CountAsync(d => d.Embedding == null, ct);

            if (missingEmbedCount == 0)
            {
                _logger.LogInformation(
                    "Knowledge base already has {Count} documents with embeddings — skipping seed.",
                    existingCount);
                return;
            }

            // Re-embed docs that have no vector yet
            _logger.LogInformation(
                "Re-embedding {Count} documents that are missing vectors...", missingEmbedCount);

            var unembedded = await _db.KnowledgeDocuments
                .Where(d => d.Embedding == null)
                .ToListAsync(ct);

            foreach (var doc in unembedded)
            {
                var embedding = await GenerateEmbeddingSafe(doc.Content, ct);
                if (embedding is not null)
                {
                    doc.Embedding = new Vector(embedding);
                    doc.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Re-embedding completed.");
            return;
        }

        // Count mismatch or empty — clear and re-seed
        if (existingCount > 0)
        {
            _logger.LogInformation(
                "Knowledge base has {Existing} docs but expected {Expected} — clearing and re-seeding.",
                existingCount, DefaultDocuments.Length);

            var all = await _db.KnowledgeDocuments.ToListAsync(ct);
            _db.KnowledgeDocuments.RemoveRange(all);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Seeding {Count} CLB knowledge documents with embeddings...",
            DefaultDocuments.Length);

        foreach (var (source, title, content) in DefaultDocuments)
        {
            await AddAsync(content, source, title, ct);
        }

        _logger.LogInformation("Knowledge base seeded successfully ({Count} documents).",
            DefaultDocuments.Length);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<float[]?> GenerateEmbeddingSafe(string text, CancellationToken ct)
    {
        try
        {
            return await _embedding.GenerateAsync(text, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding — document saved without vector.");
            return null;
        }
    }

    private static KnowledgeDocumentDto ToDto(KnowledgeDocument d) => new()
    {
        Id = d.Id,
        Content = d.Content,
        Source = d.Source,
        Title = d.Title,
        HasEmbedding = d.Embedding is not null,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };

    // ── BM25 keyword search (fallback when no embedded docs exist) ────────────
    // BM25 is the standard probabilistic ranking used by Elasticsearch/Lucene.
    // k1=1.5 (term saturation), b=0.75 (length normalization) are standard defaults.

    private static IReadOnlyList<KnowledgeChunk> KeywordFallback(string query, int topK)
    {
        var queryTerms = Tokenize(query);
        if (queryTerms.Count == 0)
            return DefaultDocuments.Take(topK)
                .Select(d => new KnowledgeChunk(d.content, d.source, 0f))
                .ToList();

        // Pre-tokenize all documents
        var tokenized = DefaultDocuments
            .Select(d => new { d.source, d.content, Terms = Tokenize(d.content) })
            .ToList();

        // Average document length (for BM25 length normalization)
        double avgDocLen = tokenized.Average(d => (double)d.Terms.Count);
        int N = tokenized.Count;

        // IDF: log((N - df + 0.5) / (df + 0.5) + 1) — BM25+ IDF formula
        double Idf(string term)
        {
            int df = tokenized.Count(d => d.Terms.Contains(term));
            return df == 0 ? 0 : Math.Log((N - df + 0.5) / (df + 0.5) + 1.0);
        }

        const double k1 = 1.5;
        const double b = 0.75;

        var ranked = tokenized.Select(doc =>
        {
            double docLen = doc.Terms.Count;
            double score = queryTerms.Sum(term =>
            {
                // TF inside the document — count occurrences
                int tf = doc.Terms.Count(t => t == term); // multiset count
                double tfNorm = (tf * (k1 + 1.0)) /
                                (tf + k1 * (1.0 - b + b * docLen / avgDocLen));
                return Idf(term) * tfNorm;
            });

            return new { doc.source, doc.content, Score = (float)score };
        })
        .OrderByDescending(x => x.Score)
        .Take(topK)
        .ToList();

        return ranked.Any(x => x.Score > 0)
            ? ranked.Where(x => x.Score > 0)
                    .Select(x => new KnowledgeChunk(x.content, x.source, x.Score))
                    .ToList()
            : DefaultDocuments.Take(topK)
                    .Select(d => new KnowledgeChunk(d.content, d.source, 0f))
                    .ToList();
    }

    // Returns a multiset (List, not HashSet) so TF counting works correctly
    private static List<string> Tokenize(string text) =>
        text.ToLower()
            .Split([' ', ',', '.', '?', '!', '\n', '\r', ':', ';', '(', ')', '/', '-'],
                StringSplitOptions.RemoveEmptyEntries)
            .ToList();

    // ── Default CLB documents (used for seed + fallback) ─────────────────────

    private static readonly (string source, string title, string content)[] DefaultDocuments =
    [
        (
            "gioi-thieu",
            "Giới thiệu Võ đường",
            """
            Võ đường Côn Nhị Khúc Hà Đông là đơn vị chuyên đào tạo môn võ binh khí Côn Nhị Khúc tại Hà Nội.
            Kể từ ngày 01/01/2026, Câu lạc bộ Côn Nhị Khúc Hà Đông đã chính thức chuyển đổi mô hình, vươn mình trở thành Võ đường Côn Nhị Khúc Hà Đông.
            Đây là cột mốc đánh dấu bước chuyển mình sang mô hình tổ chức cao cấp với hệ thống đào tạo chuẩn hóa, giáo trình quy củ và quy chế chuyên nghiệp.
            Võ đường là nơi hội tụ của những tâm hồn yêu võ thuật, nơi rèn luyện thân thể, ý chí và kết nối niềm đam mê với binh khí đoản côn tại thủ đô Hà Nội.
            Chủ nhiệm: Võ sư Nguyễn Văn Chất.
            Hotline/Zalo: 0868.699.860.
            Kênh TikTok: "Hài Code" — nơi chia sẻ kỹ thuật và những khoảnh khắc võ thuật hài hước, gần gũi.
            Võ đường hoạt động hợp pháp, có đầy đủ cơ sở pháp lý và bảo trợ pháp lý cho học viên khi di chuyển binh khí.
            """
        ),
        (
            "co-so-ha-dong",
            "Cơ sở Hà Đông",
            """
            Cơ sở Hà Đông của Võ đường Côn Nhị Khúc Hà Đông:
            Địa điểm: Trường Tiểu học Văn Yên, Hà Đông, Hà Nội.
            Lịch tập: Thứ 2 - 4 - 6, từ 18:30 đến 20:30.
            Học phí: Miễn phí.
            Đây là cơ sở trọng điểm của võ đường.
            Khung giờ cụ thể: 18:30 tập trung chuẩn bị; 18:45 bắt đầu khởi động và nội dung chuyên môn; 20:20 kết thúc tập; 20:30 ban huấn luyện nhận xét tổng kết.
            """
        ),
        (
            "co-so-kien-hung",
            "Cơ sở Kiến Hưng",
            """
            Cơ sở Kiến Hưng của Võ đường Côn Nhị Khúc Hà Đông:
            Địa điểm: Vườn hoa Hàng Bè, Kiến Hưng, Hà Đông, Hà Nội.
            Lịch tập: Thứ 3 - 5 - 7, từ 17:45 đến 19:00.
            Học phí: Miễn phí.
            """
        ),
        (
            "co-so-khac",
            "Cơ sở Thống Nhất, Hòa Bình, Kim Giang",
            """
            Võ đường Côn Nhị Khúc Hà Đông có tổng cộng 05 cơ sở tại Hà Nội:

            Cơ sở Thống Nhất:
            - Địa điểm: Công viên Thống Nhất, Hai Bà Trưng, Hà Nội.
            - Lịch tập: Thứ 3 - 5 - 7, từ 19:00 đến 21:00.
            - Học phí: 300.000đ/tháng.

            Cơ sở Hòa Bình:
            - Địa điểm: Công viên Hòa Bình, Bắc Từ Liêm, Hà Nội.
            - Lịch tập: Thứ 3 - 5 - 7, từ 19:00 đến 21:00.
            - Học phí: 300.000đ/tháng.

            Cơ sở Kim Giang:
            - Địa điểm: Sân chơi cạnh Trường Tiểu học Sao Mai, Kim Giang, Hà Nội.
            - Lịch tập: Thứ 3 - 5 - 7, từ 19:00 đến 21:00.
            - Học phí: 300.000đ/tháng.
            """
        ),
        (
            "hoc-phi-dang-ky",
            "Học phí và đăng ký",
            """
            Học phí Võ đường Côn Nhị Khúc Hà Đông:
            - Cơ sở Hà Đông (Trường TH Văn Yên): Miễn phí.
            - Cơ sở Kiến Hưng (Vườn hoa Hàng Bè): Miễn phí.
            - Cơ sở Thống Nhất, Hòa Bình, Kim Giang: 300.000đ/tháng.

            Cách đăng ký:
            - Liên hệ trực tiếp qua Hotline/Zalo: 0868.699.860 gặp Võ sư Nguyễn Văn Chất.
            - Theo dõi kênh TikTok "Hài Code" để biết thêm thông tin.
            - Học viên online ở xa cũng được đào tạo qua giáo trình bài bản và thi nâng đai trực tuyến.
            Không yêu cầu kinh nghiệm võ thuật trước. Phù hợp mọi lứa tuổi.
            """
        ),
        (
            "lo-trinh-quyen-loi",
            "Lộ trình đào tạo và quyền lợi học viên",
            """
            Võ đường áp dụng giáo trình chính quy 12 cấp bậc, đảm bảo lộ trình từ cơ bản đến nâng cao cho cả học viên Offline và Online.

            Quyền lợi khi tham gia Võ đường Côn Nhị Khúc Hà Đông:
            - Hệ thống văn bằng: Được tham gia các kỳ thi nâng đai định kỳ và cấp chứng chỉ "Chứng Nhận" khi hoàn thành cấp bậc.
            - Thẻ học viên: Được cấp thẻ chính thức để sinh hoạt tại hệ thống Võ đường.
            - Cơ hội cọ xát: Tham gia các chương trình thi đấu, biểu diễn nghệ thuật cấp Phường, Thành phố và Quốc gia.
            - Hỗ trợ Online: Học viên ở xa được đào tạo qua giáo trình bài bản và thi nâng đai theo hình thức trực tuyến.
            - Bảo trợ pháp lý: Được bảo vệ pháp lý khi di chuyển binh khí từ nhà đến điểm tập và ngược lại.
            """
        ),
        (
            "huan-luyen-vien",
            "Đội ngũ huấn luyện",
            """
            Đội ngũ huấn luyện Võ đường Côn Nhị Khúc Hà Đông:
            Đứng đầu là Võ sư Nguyễn Văn Chất — Chủ nhiệm võ đường, người sáng lập và dẫn dắt hành trình phát triển từ CLB lên Võ đường.
            Cùng đội ngũ huấn luyện viên tâm huyết, giàu kinh nghiệm thực chiến và sư phạm.
            Ban huấn luyện không chỉ dạy kỹ thuật mà còn định hướng phát triển hình ảnh cá nhân cho học viên thông qua các nền tảng mạng xã hội.
            Điển hình là kênh TikTok "Hài Code" giúp võ thuật trở nên thú vị và tiếp cận gần hơn với giới trẻ.
            Liên hệ: Hotline/Zalo 0868.699.860.
            """
        ),
        (
            "noi-quy-ky-luat",
            "Nội quy và kỷ luật võ đạo",
            """
            Võ đạo lấy kỷ luật làm gốc. Võ đường Côn Nhị Khúc Hà Đông áp dụng quy định nghiêm khắc về giờ giấc và tác phong:

            Quy định đi trễ (không thông báo trước):
            - Trễ 5–10 phút: Chạy bộ phạt (Nam 15 vòng / Nữ 10 vòng).
            - Trễ sau 19:00: Chạy bộ phạt (Nam 23 vòng / Nữ 15 vòng).

            Quy định nghỉ tập:
            - Học viên có việc riêng cần báo trước tại nhóm "Thảo luận" và điền Form báo nghỉ.
            - Tuyệt đối không nghỉ không thông báo.
            - Hình thức kỷ luật cao nhất: Đình chỉ tập luyện tại Võ đường.
            """
        ),
        (
            "thanh-tich-cong-dong",
            "Thành tích và hoạt động cộng đồng",
            """
            Thành tích và hoạt động nổi bật của Võ đường Côn Nhị Khúc Hà Đông:
            - Chính thức nâng cấp lên mô hình Võ đường vào năm 2026, khẳng định vị thế dẫn đầu trong đào tạo Côn Nhị Khúc tại Hà Nội.
            - Mạng lưới 05 cơ sở trải dài khắp Hà Nội: Hà Đông, Kiến Hưng, Thống Nhất, Hòa Bình, Kim Giang.
            - Thường xuyên tổ chức các buổi Offline tại Tượng đài Nguyễn Trãi và các công viên lớn.
            - Sứ mệnh: Phổ cập võ thuật binh khí, bảo tồn giá trị truyền thống kết hợp tinh thần hiện đại.
            - Học viên được tham gia thi đấu, biểu diễn cấp Phường, Thành phố và Quốc gia.
            """
        ),
    ];
}
