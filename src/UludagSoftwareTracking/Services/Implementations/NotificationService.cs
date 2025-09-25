using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UludagSoftwareTracking.Data;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, IEmailService emailService, ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendAsync(IEnumerable<UserProfile> recipients, string title, string message, string? link, CancellationToken cancellationToken = default)
    {
        var recipientList = recipients
            .Where(r => r is not null)
            .GroupBy(r => r!.Id)
            .Select(g => g.First()!)
            .ToList();

        if (recipientList.Count == 0)
        {
            return;
        }

        var notifications = recipientList
            .Select(r => new Notification
            {
                Title = title,
                Message = message,
                Link = link,
                RecipientUserId = r.Id,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        await _context.Notifications.AddRangeAsync(notifications, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var recipient in recipientList)
        {
            if (!string.IsNullOrWhiteSpace(recipient.Email))
            {
                await _emailService.SendEmailAsync(recipient.Email!, title, message, cancellationToken);
            }
            else
            {
                _logger.LogDebug("'{User}' kullanıcısının e-posta adresi tanımlı değil, yalnızca uygulama içi bildirim oluşturuldu.", recipient.UserName);
            }
        }
    }

    public async Task<IReadOnlyList<NotificationViewModel>> GetUnreadNotificationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return notifications
            .Select(n => new NotificationViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Link = n.Link,
                CreatedAt = n.CreatedAt
            })
            .ToList();
    }

    public async Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId, cancellationToken);

        if (notification is null)
        {
            return;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
