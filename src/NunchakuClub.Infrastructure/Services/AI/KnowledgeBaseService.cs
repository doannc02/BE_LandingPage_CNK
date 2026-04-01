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
///   1. Generate query embedding via IEmbeddingService (Google gemini-embedding-001)
///   2. ORDER BY embedding <=> queryVector (cosine distance) via HNSW index
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
        string query, int topK = 3, CancellationToken ct = default)
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
            ?? throw new InvalidOperationException( $"Document {id} not found.");

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
            ?? throw new InvalidOperationException( $"Document {id} not found.");

        _db.KnowledgeDocuments.Remove(doc);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted knowledge document {Id}", id);
    }

    public async Task SeedDefaultAsync(CancellationToken ct = default)
    {
        var existingCount = await _db.KnowledgeDocuments.CountAsync(ct);

        if (existingCount == DefaultDocuments.Length)
        {
            var missingEmbedCount = await _db.KnowledgeDocuments
                .CountAsync(d => d.Embedding == null, ct);

            if (missingEmbedCount == 0)
            {
                _logger.LogInformation(
                    "Knowledge base already has {Count} documents with embeddings — skipping seed.",
                    existingCount);
                return;
            }

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

    // ── BM25 keyword search (fallback) ────────────────────────────────────────

    private static IReadOnlyList<KnowledgeChunk> KeywordFallback(string query, int topK)
    {
        var queryTerms = Tokenize(query);
        if (queryTerms.Count == 0)
            return DefaultDocuments.Take(topK)
                .Select(d => new KnowledgeChunk(d.content, d.source, 0f))
                .ToList();

        var tokenized = DefaultDocuments
            .Select(d => new { d.source, d.content, Terms = Tokenize(d.content) })
            .ToList();

        double avgDocLen = tokenized.Average(d => (double)d.Terms.Count);
        int N = tokenized.Count;

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
                int tf = doc.Terms.Count(t => t == term);
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

    private static List<string> Tokenize(string text) =>
        text.ToLower()
            .Split([' ', ',', '.', '?', '!', '\n', '\r', ':', ';', '(', ')', '/', '-'],
                StringSplitOptions.RemoveEmptyEntries)
            .ToList();

    // ── Default documents (seed + BM25 fallback) ──────────────────────────────

    private static readonly (string source, string title, string content)[] DefaultDocuments =
    [
        (
            "gioi-thieu",
            "Giới thiệu Võ đường",
            """
            Võ đường Côn Nhị Khúc Hà Đông là đơn vị chuyên đào tạo môn võ binh khí Côn Nhị Khúc tại Hà Nội, được thành lập và hoạt động gần 15 năm.
            Kể từ ngày 01/01/2026, Câu lạc bộ Côn Nhị Khúc Hà Đông chính thức chuyển đổi mô hình, trở thành Võ đường Côn Nhị Khúc Hà Đông — bước chuyển sang mô hình tổ chức cao cấp với hệ thống đào tạo chuẩn hóa, giáo trình quy củ và quy chế chuyên nghiệp.
            Võ đường lấy tinh thần Võ đạo làm nền tảng, lấy Nhân – Chí – Dũng làm tôn chỉ: Nhân để rèn đạo đức nhân cách, Chí để hun đúc ý chí nghị lực, Dũng để mạnh mẽ đối mặt thử thách.
            Chủ nhiệm: Võ sư Nguyễn Văn Chất.
            Hotline/Zalo Chủ nhiệm: 0868.699.860.
            Hotline/Zalo Thư ký (Ngọc Diệu): 0862.515.596.
            Fanpage: Côn Nhị Khúc Hà Đông.
            TikTok: Côn Nhị Khúc Hà Đông (kênh "Hài Code" — chia sẻ kỹ thuật và khoảnh khắc võ thuật gần gũi với giới trẻ).
            Võ đường hoạt động hợp pháp, có đầy đủ cơ sở pháp lý và bảo trợ pháp lý cho học viên khi di chuyển binh khí.
            """
        ),
        (
            "co-so-ha-dong",
            "Cơ sở Hà Đông",
            """
            Cơ sở 1 – Hà Đông (cơ sở trọng điểm của Võ đường Côn Nhị Khúc Hà Đông):
            Địa điểm: Trường Tiểu học Văn Yên, Hà Đông, Hà Nội.
            Lịch tập: Thứ 2 - 4 - 6 hàng tuần.
            Giờ tập: 18:30 – 20:30.
            Học phí: Miễn phí.
            Khung giờ chi tiết:
            - 18:30: Tập trung chuẩn bị.
            - 18:45: Bắt đầu khởi động và nội dung chuyên môn.
            - 20:20: Kết thúc tập.
            - 20:30: Ban huấn luyện nhận xét tổng kết buổi tập.
            """
        ),
        (
            "co-so-kien-hung",
            "Cơ sở Kiến Hưng",
            """
            Cơ sở 2 – Kiến Hưng của Võ đường Côn Nhị Khúc Hà Đông:
            Địa điểm: Nhà văn hóa Tổ 19, phường Kiến Hưng, Hà Đông, Hà Nội.
            Lịch tập: Thứ 3 - 5 - 7 hàng tuần.
            Giờ tập: 18:00 – 19:45.
            Học phí: 350.000đ/tháng.
            """
        ),
        (
            "co-so-thong-nhat",
            "Cơ sở Thống Nhất",
            """
            Cơ sở 3 – Thống Nhất của Võ đường Côn Nhị Khúc Hà Đông:
            Địa điểm: Công viên Thống Nhất, Hai Bà Trưng, Hà Nội (sau tượng đài công an nhân dân).
            Lịch tập: Thứ 3 - 5 - 7 hàng tuần.
            Giờ tập: 19:00 – 21:00.
            Học phí: 300.000đ/tháng.
            """
        ),
        (
            "co-so-hoa-binh",
            "Cơ sở Hòa Bình",
            """
            Cơ sở 4 – Hòa Bình của Võ đường Côn Nhị Khúc Hà Đông:
            Địa điểm: Công viên Hòa Bình, Cổ Nhuế, Bắc Từ Liêm, Hà Nội.
            Lịch tập: Thứ 3 - 5 - 7 hàng tuần.
            Giờ tập: 19:00 – 21:00.
            Học phí: 300.000đ/tháng.
            """
        ),
        (
            "co-so-kim-giang",
            "Cơ sở Kim Giang",
            """
            Cơ sở 5 – Kim Giang của Võ đường Côn Nhị Khúc Hà Đông:
            Địa điểm: Công viên đối diện Cổng số 4 Trường Ngôi sao Hoàng Mai, Ngõ 350 Kim Giang, Hà Nội.
            Lịch tập: Thứ 3 - 5 - 7 hàng tuần.
            Giờ tập: 19:00 – 21:00.
            Học phí: 300.000đ/tháng.
            """
        ),
        (
            "tong-hop-co-so",
            "Tổng hợp 5 cơ sở tập luyện",
            """
            Võ đường Côn Nhị Khúc Hà Đông có 5 cơ sở tập luyện tại Hà Nội:

            1. Cơ sở Hà Đông: Trường TH Văn Yên – Thứ 2-4-6 – 18:30-20:30 – Miễn phí.
            2. Cơ sở Kiến Hưng: Nhà văn hóa Tổ 19 phường Kiến Hưng – Thứ 3-5-7 – 18:00-19:45 – 350.000đ/tháng.
            3. Cơ sở Thống Nhất: Công viên Thống Nhất, Hai Bà Trưng – Thứ 3-5-7 – 19:00-21:00 – 300.000đ/tháng.
            4. Cơ sở Hòa Bình: Công viên Hòa Bình, Cổ Nhuế, Bắc Từ Liêm – Thứ 3-5-7 – 19:00-21:00 – 300.000đ/tháng.
            5. Cơ sở Kim Giang: Công viên đối diện Cổng số 4 Trường Ngôi sao Hoàng Mai, Ngõ 350 Kim Giang – Thứ 3-5-7 – 19:00-21:00 – 300.000đ/tháng.

            Liên hệ đăng ký:
            - Chủ nhiệm Nguyễn Văn Chất: 0868.699.860 (Zalo/Hotline).
            - Thư ký Ngọc Diệu: 0862.515.596 (Zalo/Hotline).
            """
        ),
        (
            "hoc-phi-dang-ky",
            "Học phí và cách đăng ký",
            """
            Học phí Võ đường Côn Nhị Khúc Hà Đông:
            - Cơ sở Hà Đông (Trường TH Văn Yên): Miễn phí.
            - Cơ sở Kiến Hưng (Nhà văn hóa Tổ 19): 350.000đ/tháng.
            - Cơ sở Thống Nhất, Hòa Bình, Kim Giang: 300.000đ/tháng.

            Cách đăng ký tham gia:
            - Liên hệ Chủ nhiệm: Võ sư Nguyễn Văn Chất – Hotline/Zalo: 0868.699.860.
            - Liên hệ Thư ký: Ngọc Diệu – Hotline/Zalo: 0862.515.596.
            - Theo dõi Fanpage: Côn Nhị Khúc Hà Đông.
            - Theo dõi TikTok: Côn Nhị Khúc Hà Đông.
            - Không yêu cầu kinh nghiệm võ thuật trước. Phù hợp mọi lứa tuổi.
            - Học viên online ở xa được đào tạo qua giáo trình bài bản và thi nâng đai trực tuyến.
            """
        ),
        (
            "hoc-online",
            "Khóa học Online và quyền lợi học viên Online",
            """
            Võ đường Côn Nhị Khúc Hà Đông có khóa học Online theo giáo trình chính quy 12 cấp bậc.

            Quyền lợi học viên Online:
            1. Tập luyện theo giáo trình chính quy 12 cấp bậc — giống 100% học viên Offline.
            2. Được tham gia các kỳ thi nâng đai (hình thức Online nếu ở xa).
            3. Được cấp chứng chỉ "Chứng Nhận" khi lên đai thành công.
            4. Được cấp "Thẻ Học Viên" chính thức.
            5. Được tham gia thi đấu, biểu diễn nghệ thuật cấp Phường, Thành phố, Quốc gia.
            6. Được tặng 1 cây côn xốp khi đăng ký trước ngày 01/01/2025.
            7. Nếu có điều kiện, học viên Online có thể đến tập tại các cơ sở Offline mà không cần đóng thêm phí.
            8. Sau 3 tháng chưa đạt yêu cầu, Võ đường hỗ trợ thêm đến khi đạt.

            Hình thức học:
            - Học trực tiếp qua Zoom, HLV đứng dạy trực tiếp.
            - Nếu vắng mặt, có video gửi lại để xem lại.
            - Giáo trình không giới hạn: học viên tiến bộ nhanh sẽ được dạy thêm phần phụ lục của cấp tiếp theo.
            - Điều kiện thi nâng đai: phải có võ phục.

            Liên hệ đăng ký Online:
            - Ngọc Diệu: 0862.515.596 (Zalo).
            - Chất Nguyễn: 0868.699.860 (Zalo).
            """
        ),
        (
            "lo-trinh-quyen-loi",
            "Lộ trình đào tạo và quyền lợi học viên",
            """
            Võ đường áp dụng giáo trình chính quy 12 cấp bậc, đảm bảo lộ trình từ cơ bản đến nâng cao cho cả học viên Offline và Online.
            Học viên bắt đầu từ cấp 1 (không cần kinh nghiệm trước) và tiến dần qua 12 cấp bậc.
            Sau mỗi 3 tháng, học viên có thể đăng ký thi nâng đai nếu đủ điều kiện.
            Bắt buộc thi nâng đai để học giáo trình cấp tiếp theo.

            Quyền lợi khi tham gia:
            - Văn bằng: Thi nâng đai định kỳ, cấp chứng chỉ "Chứng Nhận" khi hoàn thành cấp bậc.
            - Thẻ học viên: Được cấp thẻ chính thức sinh hoạt tại hệ thống Võ đường.
            - Thi đấu & biểu diễn: Tham gia các chương trình cấp Phường, Thành phố và Quốc gia.
            - Hỗ trợ Online: Học viên ở xa được đào tạo và thi nâng đai trực tuyến.
            - Bảo trợ pháp lý: Được bảo vệ pháp lý khi di chuyển binh khí từ nhà đến điểm tập và ngược lại.
            """
        ),
        (
            "huan-luyen-vien",
            "Đội ngũ huấn luyện",
            """
            Đội ngũ huấn luyện Võ đường Côn Nhị Khúc Hà Đông:
            - Chủ nhiệm: Võ sư Nguyễn Văn Chất — người sáng lập, dẫn dắt hành trình phát triển từ CLB lên Võ đường sau gần 15 năm.
            - Đội ngũ huấn luyện viên tâm huyết, giàu kinh nghiệm thực chiến và sư phạm.
            - Ban huấn luyện không chỉ dạy kỹ thuật mà còn định hướng phát triển hình ảnh cá nhân cho học viên qua mạng xã hội.
            - Kênh TikTok "Hài Code" và "Côn Nhị Khúc Hà Đông" giúp võ thuật trở nên thú vị, gần gũi với giới trẻ.
            Liên hệ: Hotline/Zalo 0868.699.860 (Võ sư Nguyễn Văn Chất).
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
            - Gần 15 năm hình thành và phát triển dưới sự bảo trợ của chính quyền địa phương.
            - Chính thức nâng cấp lên mô hình Võ đường vào ngày 01/01/2026.
            - Năm 2025: Tổ chức 3 kỳ thi nâng đai với số lượng học viên tăng từ 7 → 24 → 42 người.
            - Mạng lưới 05 cơ sở trải dài khắp Hà Nội: Hà Đông, Kiến Hưng, Thống Nhất, Hòa Bình, Kim Giang.
            - Thường xuyên tổ chức các buổi Offline tại Tượng đài Nguyễn Trãi và các công viên lớn.
            - Học viên được tham gia thi đấu, biểu diễn cấp Phường, Thành phố và Quốc gia.
            - Sứ mệnh: Phổ cập võ thuật binh khí, bảo tồn giá trị truyền thống kết hợp tinh thần hiện đại.
            """
        ),
        (
            "phap-ly-bao-tro",
            "Pháp lý và bảo trợ học viên",
            """
            Võ đường Côn Nhị Khúc Hà Đông là đơn vị hoạt động hợp pháp, có đầy đủ cơ sở pháp lý để tổ chức tập luyện, đào tạo và sinh hoạt võ thuật tại các cơ sở Võ đường Côn Nhị Khúc Hà Đông là đơn vị hoạt động hợp pháp, có đầy đủ cơ sở pháp lý để tổ chức tập luyện, đào tạo và sinh hoạt võ thuật tại các cơ sở trên địa bàn Hà Nội.
            Cam kết pháp lý và bảo trợ:
            - Bảo trợ hợp pháp cho toàn bộ học viên đang tham gia tập luyện trong hệ thống.
            - Học viên được bảo trợ về mặt pháp lý khi mang theo côn trong quá trình di chuyển từ nhà đến địa điểm tập luyện và ngược lại.
            - Tất cả hoạt động tập luyện tuân thủ quy định pháp luật, đảm bảo an toàn, đúng mục đích võ thuật, thể thao và văn hóa.

            Lưu ý:
            - Nội dung bảo trợ pháp lý áp dụng theo hướng dẫn của Võ đường và quy định hiện hành.
            - Khi cần xác nhận, học viên nên liên hệ trực tiếp Ban huấn luyện để được hỗ trợ.
            """
        ),
        (
            "hoi-dap-nhanh",
            "Hỏi đáp nhanh (FAQ)",
            """
            Hỏi: Võ đường có mấy cơ sở?
            Đáp: Võ đường có 5 cơ sở tập luyện tại Hà Nội: Hà Đông, Kiến Hưng, Thống Nhất, Hòa Bình, Kim Giang.

            Hỏi: Có cần biết võ trước khi học không?
            Đáp: Không yêu cầu kinh nghiệm trước. Học viên bắt đầu từ giáo trình cơ bản.

            Hỏi: Học phí thế nào?
            Đáp:
            - Hà Đông: Miễn phí.
            - Kiến Hưng: 350.000đ/tháng.
            - Thống Nhất, Hòa Bình, Kim Giang: 300.000đ/tháng.

            Hỏi: Online có thi nâng đai được không?
            Đáp: Có. Online có thể thi theo hình thức Online nếu ở xa (theo thông báo của Võ đường).

            Hỏi: Liên hệ đăng ký ở đâu?
            Đáp:
            - Chủ nhiệm Nguyễn Văn Chất: 0868.699.860 (Zalo/Hotline)
            - Thư ký Ngọc Diệu: 0862.515.596 (Zalo/Hotline)
            - Fanpage: Côn Nhị Khúc Hà Đông
            - TikTok: Côn Nhị Khúc Hà Đông
            """
        ),
        (
            "cau-tra-loi-mau",
            "Quy tắc trả lời (để tránh bịa thông tin)",
            """
            Quy tắc trả lời cho trợ lý AI của Võ đường:
            - Chỉ trả lời bằng tiếng Việt, giọng điệu thân thiện và ngắn gọn.
            - Chỉ sử dụng thông tin nằm trong kho kiến thức (knowledge base).
            - Nếu câu hỏi nằm ngoài kho kiến thức, trả lời theo mẫu:
              "Xin lỗi, tôi chưa có thông tin về vấn đề này. Bạn vui lòng liên hệ hotline/Zalo 0868.699.860 hoặc nhắn tin Fanpage để được hỗ trợ nhanh nhất nhé!"
            - Không suy đoán thông tin địa chỉ, học phí, lịch tập nếu không có dữ liệu.
            """
        )
    ];
}
