using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UludagSoftwareTracking.Models.ViewModels;
using UludagSoftwareTracking.Services.Interfaces;
using UludagSoftwareTracking.Services.Security;

namespace UludagSoftwareTracking.Controllers;

[Authorize(Policy = RoleConstants.Policies.RequireBaskan)]
public class SoftwareManagementController : Controller
{
    private readonly ISoftwareManagementService _softwareManagementService;

    public SoftwareManagementController(ISoftwareManagementService softwareManagementService)
    {
        _softwareManagementService = softwareManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Yonet(CancellationToken cancellationToken)
    {
        var model = await _softwareManagementService.GetManagementViewModelAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(SoftwareEditInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Warning"] = "Lütfen yazılım bilgilerini kontrol edin.";
            return RedirectToAction(nameof(Yonet));
        }

        try
        {
            await _softwareManagementService.CreateSoftwareAsync(model, cancellationToken);
            TempData["Success"] = "Yazılım başarıyla eklendi.";
        }
        catch (Exception ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Yonet));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guncelle(SoftwareEditInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            TempData["Warning"] = "Yazılım bilgileri geçerli değil.";
            return RedirectToAction(nameof(Yonet));
        }

        try
        {
            await _softwareManagementService.UpdateSoftwareAsync(model, cancellationToken);
            TempData["Success"] = "Yazılım bilgileri güncellendi.";
        }
        catch (Exception ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Yonet));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _softwareManagementService.DeleteSoftwareAsync(id, cancellationToken);
            TempData["Success"] = "Yazılım kaydı silindi.";
        }
        catch (Exception ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Yonet));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SorumlulukGuncelle(SoftwareResponsibilityInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Warning"] = "Sorumlu güncelleme bilgileri eksik.";
            return RedirectToAction(nameof(Yonet));
        }

        try
        {
            await _softwareManagementService.UpdateResponsibilitiesAsync(model, cancellationToken);
            TempData["Success"] = "Sorumlu listesi güncellendi.";
        }
        catch (Exception ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Yonet));
    }
}
