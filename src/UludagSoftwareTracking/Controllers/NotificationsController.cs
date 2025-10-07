using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly IUserContextService _userContextService;

    public NotificationsController(INotificationService notificationService, IUserContextService userContextService)
    {
        _notificationService = notificationService;
        _userContextService = userContextService;
    }

    public sealed class NotificationReadRequest
    {
        public int Id { get; init; }
    }

    [HttpPost("Okundu")]
    public async Task<IActionResult> Okundu([FromBody] NotificationReadRequest request, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (request?.Id > 0)
        {
            await _notificationService.MarkAsReadAsync(request.Id, user.Id, cancellationToken);
        }

        return Ok();
    }
}
