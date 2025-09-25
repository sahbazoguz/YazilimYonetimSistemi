using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface INotificationService
{
    Task SendAsync(IEnumerable<UserProfile> recipients, string title, string message, string? link, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationViewModel>> GetUnreadNotificationsAsync(int userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
}
