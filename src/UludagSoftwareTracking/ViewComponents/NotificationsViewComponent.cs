using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UludagSoftwareTracking.Models.ViewModels;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.ViewComponents;

public class NotificationsViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;
    private readonly IUserContextService _userContextService;

    public NotificationsViewComponent(INotificationService notificationService, IUserContextService userContextService)
    {
        _notificationService = notificationService;
        _userContextService = userContextService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return View(Array.Empty<NotificationViewModel>());
        }

        var notifications = await _notificationService.GetUnreadNotificationsAsync(user.Id, cancellationToken);
        return View(notifications);
    }
}
