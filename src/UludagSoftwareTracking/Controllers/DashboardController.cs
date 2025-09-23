using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IUserContextService _userContextService;

    public DashboardController(IDashboardService dashboardService, IUserContextService userContextService)
    {
        _dashboardService = dashboardService;
        _userContextService = userContextService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        var dashboard = await _dashboardService.GetDashboardAsync(user, cancellationToken);
        return View("~/Views/Home/Index.cshtml", dashboard);
    }
}
