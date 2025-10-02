using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;
using UludagSoftwareTracking.Services.Interfaces;
using UludagSoftwareTracking.Services.Security;

namespace UludagSoftwareTracking.Controllers;

[Authorize]
public class ManualsController : Controller
{
    private readonly IManualService _manualService;
    private readonly IUserContextService _userContextService;

    public ManualsController(IManualService manualService, IUserContextService userContextService)
    {
        _manualService = manualService;
        _userContextService = userContextService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (!KullaniciKilavuzYetkisineSahip(user))
        {
            return Forbid();
        }

        var manuals = await _manualService.GetManualsAsync(user.Id, cancellationToken);
        return View(manuals);
    }

    public async Task<IActionResult> Yukle(int? softwareId, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (!KullaniciKilavuzYetkisineSahip(user))
        {
            return Forbid();
        }

        var viewModel = await _manualService.GetManualUploadModelAsync(user.Id, softwareId, cancellationToken);
        if (!viewModel.Yazilimlar.Any())
        {
            TempData["Warning"] = "Yetkili olduğunuz bir yazılım bulunmuyor.";
        }
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yukle(ManualUploadViewModel model, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (!KullaniciKilavuzYetkisineSahip(user))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var refreshed = await _manualService.GetManualUploadModelAsync(user.Id, model.SoftwareId, cancellationToken);
            refreshed.Title = model.Title;
            refreshed.FilePath = model.FilePath;
            refreshed.ManualType = model.ManualType;
            refreshed.Version = model.Version;
            refreshed.ChangeLog = model.ChangeLog;
            return View(refreshed);
        }

        var isDeveloper = user.Role == UserRole.Yazilimci;
        var isUnitUser = user.Role == UserRole.BirimKullanicisi;
        var isUnitManager = user.Role == UserRole.BirimYetkilisi;

        if (model.ManualType == ManualType.KullanimKilavuzu && !(isUnitUser || isUnitManager))
        {
            TempData["Warning"] = "Kullanım kılavuzu yalnızca talep sahibi birim tarafından yüklenebilir.";
            return RedirectToAction(nameof(Index));
        }

        if (model.ManualType == ManualType.TeknikKilavuz && !isDeveloper)
        {
            TempData["Warning"] = "Teknik kılavuz yalnızca geliştiriciler tarafından yüklenebilir.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _manualService.SaveManualAsync(model, user.Id, cancellationToken);
            TempData["Success"] = "Kılavuz başarıyla kaydedildi";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool KullaniciKilavuzYetkisineSahip(UserProfile? user)
    {
        if (user is null)
        {
            return false;
        }

        return user.Role == UserRole.BirimKullanicisi ||
               user.Role == UserRole.BirimYetkilisi ||
               user.Role == UserRole.Yazilimci;
    }
}
