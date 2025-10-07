using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UludagSoftwareTracking.Models.ViewModels;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IUserContextService _userContextService;

    public HomeController(IDashboardService dashboardService, IUserContextService userContextService)
    {
        _dashboardService = dashboardService;
        _userContextService = userContextService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var currentUser = await _userContextService.GetCurrentUserAsync(cancellationToken);
        var dashboard = await _dashboardService.GetDashboardAsync(currentUser, cancellationToken);
        return View(dashboard);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Hata()
    {
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
