using System;
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

[Authorize(Policy = RoleConstants.Policies.RequireBaskan)]
public class DepartmentsController : Controller
{
    private readonly IDepartmentService _departmentService;
    private readonly IUserProfileService _userProfileService;

    public DepartmentsController(IDepartmentService departmentService, IUserProfileService userProfileService)
    {
        _departmentService = departmentService;
        _userProfileService = userProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> Yonet(CancellationToken cancellationToken)
    {
        var model = await BuildViewModelAsync(null, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(DepartmentInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnManageViewWithModelStateAsync(cancellationToken, model);
        }

        try
        {
            await _departmentService.CreateAsync(new Department
            {
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
            }, cancellationToken);
            TempData["Success"] = "Birim başarıyla eklendi.";
        }
        catch (Exception ex)
        {
            TempData["Warning"] = $"Birim eklenemedi: {ex.Message}";
        }

        return RedirectToAction(nameof(Yonet));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guncelle(DepartmentUpdateInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Warning"] = "Lütfen birim bilgilerini kontrol edin.";
            return RedirectToAction(nameof(Yonet));
        }

        try
        {
            await _departmentService.UpdateAsync(new Department
            {
                Id = model.Id,
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
            }, cancellationToken);

            TempData["Success"] = "Birim bilgileri güncellendi.";
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
            await _departmentService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Birim silindi.";
        }
        catch (Exception ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Yonet));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YetkiliAta(DepartmentAssignmentInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Warning"] = "Lütfen atama bilgilerini eksiksiz doldurun.";
            return RedirectToAction(nameof(Yonet));
        }

        if (model.Role is not (UserRole.BirimYetkilisi or UserRole.BirimKullanicisi))
        {
            TempData["Warning"] = "Sadece Birim Yetkilisi veya Birim Kullanıcısı atanabilir.";
            return RedirectToAction(nameof(Yonet));
        }

        var department = await _departmentService.GetByIdAsync(model.DepartmentId, cancellationToken);
        if (department is null)
        {
            TempData["Warning"] = "Birim bulunamadı.";
            return RedirectToAction(nameof(Yonet));
        }

        var user = await _userProfileService.GetByUserNameAsync(model.UserName, cancellationToken);
        if (user is null)
        {
            TempData["Warning"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Yonet));
        }

        if (user.Role == UserRole.Admin)
        {
            TempData["Warning"] = "Yönetici rolündeki kullanıcılar yetkilendirilemez.";
            return RedirectToAction(nameof(Yonet));
        }

        if (user.DepartmentId == model.DepartmentId && user.Role == model.Role)
        {
            TempData["Warning"] = "Kullanıcı zaten bu birimde görevlendirilmiş.";
            return RedirectToAction(nameof(Yonet));
        }

        var uyari = user.DepartmentId.HasValue && user.DepartmentId != model.DepartmentId
            ? $"Kullanıcı {user.Department?.Name ?? "başka birim"} biriminden alınarak {department.Name} birimine taşındı."
            : null;

        await _userProfileService.UpdateRoleAsync(user.UserName, model.Role, model.DepartmentId, cancellationToken);
        TempData["Success"] = uyari is null
            ? $"{user.FullName} için görevlendirme yapıldı."
            : $"{user.FullName} için görevlendirme yapıldı. {uyari}";
        return RedirectToAction(nameof(Yonet));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GorevdenAl(string userName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            TempData["Warning"] = "Kullanıcı adı gerekli.";
            return RedirectToAction(nameof(Yonet));
        }

        var user = await _userProfileService.GetByUserNameAsync(userName, cancellationToken);
        if (user is null)
        {
            TempData["Warning"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Yonet));
        }

        if (user.Role is not (UserRole.BirimYetkilisi or UserRole.BirimKullanicisi))
        {
            TempData["Warning"] = "Bu kullanıcının görevi bulunmuyor.";
            return RedirectToAction(nameof(Yonet));
        }

        await _userProfileService.UpdateRoleAsync(user.UserName, UserRole.Personel, null, cancellationToken);
        TempData["Success"] = $"{user.FullName} için görev kaldırıldı.";
        return RedirectToAction(nameof(Yonet));
    }

    private async Task<DepartmentManagementViewModel> BuildViewModelAsync(
        DepartmentInputModel? yeniBirim,
        CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetDepartmentsAsync(cancellationToken);
        var users = await _userProfileService.GetAllAsync(cancellationToken);

        return new DepartmentManagementViewModel
        {
            Departments = departments.Select(MapRow).ToList(),
            YeniBirim = yeniBirim ?? new DepartmentInputModel(),
            KullaniciSecenekleri = users
                .Where(u => u.Role != UserRole.Admin)
                .Where(u => u.IsActive)
                .Select(MapOption)
                .OrderBy(u => u.FullName)
                .ToList()
        };
    }

    private static DepartmentManagementRowViewModel MapRow(Department department)
    {
        return new DepartmentManagementRowViewModel
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            ContactEmail = department.ContactEmail,
            PhoneNumber = department.PhoneNumber,
            Yetkililer = department.Users
                .Where(u => u.Role == UserRole.BirimYetkilisi)
                .OrderBy(u => u.FullName)
                .Select(ToSummary)
                .ToList(),
            Kullanicilar = department.Users
                .Where(u => u.Role == UserRole.BirimKullanicisi)
                .OrderBy(u => u.FullName)
                .Select(ToSummary)
                .ToList()
        };
    }

    private static UserAssignmentOptionViewModel MapOption(UserProfile user)
    {
        return new UserAssignmentOptionViewModel
        {
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            CurrentRole = user.Role,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name
        };
    }

    private static UserSummaryViewModel ToSummary(UserProfile user)
    {
        return new UserSummaryViewModel
        {
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    private async Task<IActionResult> ReturnManageViewWithModelStateAsync(CancellationToken cancellationToken, DepartmentInputModel yeniBirim)
    {
        var model = await BuildViewModelAsync(yeniBirim, cancellationToken);
        return View("Yonet", model);
    }
}
