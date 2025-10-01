using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        var departments = await _departmentService.GetDepartmentsAsync(cancellationToken);
        var users = await _userProfileService.GetAllAsync(cancellationToken);

        var model = new DepartmentManagementViewModel
        {
            Departments = departments
                .Select(MapRow)
                .ToList(),
            YeniBirim = new DepartmentInputModel(),
            KullaniciSecenekleri = users
                .Select(u => new SelectListItem
                {
                    Value = u.UserName,
                    Text = $"{u.FullName} ({u.UserName}) - {u.Role}"
                })
                .ToList()
        };

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
            var department = new Department
            {
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
            };

            await _departmentService.CreateAsync(department, cancellationToken);
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

        await _userProfileService.UpdateRoleAsync(user.UserName, model.Role, model.DepartmentId, cancellationToken);
        TempData["Success"] = $"{user.FullName} için rol güncellendi.";
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
        var departments = await _departmentService.GetDepartmentsAsync(cancellationToken);
        var users = await _userProfileService.GetAllAsync(cancellationToken);

        var model = new DepartmentManagementViewModel
        {
            Departments = departments.Select(MapRow).ToList(),
            YeniBirim = yeniBirim,
            KullaniciSecenekleri = users
                .Select(u => new SelectListItem
                {
                    Value = u.UserName,
                    Text = $"{u.FullName} ({u.UserName}) - {u.Role}"
                })
                .ToList()
        };

        return View("Yonet", model);
    }
}
