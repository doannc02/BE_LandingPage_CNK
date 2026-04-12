using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Interfaces;

public interface IFirebaseChatService
{
    /// <summary>
    /// Truy vấn Firebase RTDB: lấy tất cả chat rooms có status="open",
    /// GroupBy theo adminId → trả về Dictionary&lt;adminId, openChatCount&gt;.
    /// Dùng cho chiến lược Least-Loaded khi điều phối admin.
    /// </summary>
    Task<Dictionary<string, int>> GetAdminWorkloadsAsync(CancellationToken ct = default);

    /// <summary>
    /// Tạo chat room trong Firebase Realtime Database, ghi metadata và message đầu tiên của user.
    /// Trả về chatRoomId để frontend subscribe realtime.
    /// </summary>
    Task<string> CreateChatRoomAsync(
        string sessionId,
        string adminId,
        string firstMessage,
        CancellationToken ct = default);

    /// <summary>
    /// Push thêm một message vào chat room đang active.
    /// Dùng khi admin reply qua backend (thay vì Firebase SDK).
    /// </summary>
    Task SendMessageAsync(
        string chatId,
        string sender,
        string text,
        CancellationToken ct = default);

    /// <summary>Đóng chat room, cập nhật status = "closed" trong Firebase.</summary>
    Task CloseChatAsync(string chatId, CancellationToken ct = default);

    /// <summary>
    /// Ghi notification tới user session khi admin reply pending message (user offline).
    /// Path: /notifications/{sessionId}
    /// </summary>
    Task NotifyUserReplyAsync(
        string sessionId,
        string adminReply,
        string adminId,
        CancellationToken ct = default);
}
