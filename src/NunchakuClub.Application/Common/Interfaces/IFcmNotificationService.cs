using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Interfaces;

public interface IFcmNotificationService
{
    /// <summary>Gửi push notification đến tất cả admin khi có pending message từ user.</summary>
    Task NotifyAllAdminsAsync(string userMessage, Guid pendingMessageId, CancellationToken ct = default);
}
