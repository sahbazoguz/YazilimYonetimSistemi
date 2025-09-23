using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        if (!KullaniciKilavuzYetkisineSahip())
        {
            return Forbid();
        }

        var manuals = await _manualService.GetManualsAsync(cancellationToken);
        return View(manuals);
    }

    public async Task<IActionResult> Yukle(int? softwareId, CancellationToken cancellationToken)
    {
        if (!KullaniciKilavuzYetkisineSahip())
        {
            return Forbid();
        }

        var viewModel = await _manualService.GetManualUploadModelAsync(softwareId, cancellationToken);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yukle(ManualUploadViewModel model, CancellationToken cancellationToken)
    {
        if (!KullaniciKilavuzYetkisineSahip())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var refreshed = await _manualService.GetManualUploadModelAsync(model.SoftwareId, cancellationToken);
            refreshed.Title = model.Title;
            refreshed.FilePath = model.FilePath;
            refreshed.ManualType = model.ManualType;
            refreshed.Version = model.Version;
            refreshed.ChangeLog = model.ChangeLog;
            return View(refreshed);
        }

        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        await _manualService.SaveManualAsync(model, user.Id, cancellationToken);
        TempData["Success"] = "Kılavuz başarıyla kaydedildi";
        return RedirectToAction(nameof(Index));
    }

    private bool KullaniciKilavuzYetkisineSahip()
    {
        return User.IsInRole(RoleConstants.Roles.BirimKullanicisi) ||
               User.IsInRole(RoleConstants.Roles.BirimYetkilisi) ||
               User.IsInRole(RoleConstants.Roles.YazilimEkibiLideri) ||
               User.IsInRole(RoleConstants.Roles.Yazilimci) ||
               User.IsInRole(RoleConstants.Roles.Admin);
    }
}
