using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Controllers;

public class SoftwareController : Controller
{
    private readonly ISoftwareCatalogService _catalogService;

    public SoftwareController(ISoftwareCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IActionResult> Index(string? aramaMetni, int? birimId, CancellationToken cancellationToken)
    {
        var viewModel = await _catalogService.GetCatalogAsync(aramaMetni, birimId, cancellationToken);
        return View(viewModel);
    }

    public async Task<IActionResult> Detay(int id, CancellationToken cancellationToken)
    {
        var viewModel = await _catalogService.GetDetailAsync(id, cancellationToken);
        if (viewModel is null)
        {
            return NotFound();
        }

        return View(viewModel);
    }
}
