using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UludagSoftwareTracking.Models.ViewModels;
using UludagSoftwareTracking.Services.Interfaces;
using UludagSoftwareTracking.Services.Security;

namespace UludagSoftwareTracking.Controllers;

[Authorize(Policy = RoleConstants.Policies.RequireAdmin)]
public class AdminController : Controller
{
    private readonly IUserProfileService _userProfileService;
    private readonly IDepartmentService _departmentService;

    public AdminController(IUserProfileService userProfileService, IDepartmentService departmentService)
    {
        _userProfileService = userProfileService;
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _userProfileService.GetAllAsync(cancellationToken);
        return View(users);
    }

    public async Task<IActionResult> RolAta(string? userName, CancellationToken cancellationToken)
    {
        var users = await _userProfileService.GetAllAsync(cancellationToken);
        var departments = await _departmentService.GetDepartmentsAsync(cancellationToken);

        var model = new AdminRoleAssignmentViewModel
        {
            KullaniciListesi = users,
            Departments = departments,
            UserName = userName ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var selectedUser = users.FirstOrDefault(u => u.UserName == userName);
            if (selectedUser is not null)
            {
                model.Role = selectedUser.Role;
                model.DepartmentId = selectedUser.DepartmentId;
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RolAta(AdminRoleAssignmentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.KullaniciListesi = await _userProfileService.GetAllAsync(cancellationToken);
            model.Departments = await _departmentService.GetDepartmentsAsync(cancellationToken);
            return View(model);
        }

        await _userProfileService.UpdateRoleAsync(model.UserName, model.Role, model.DepartmentId, cancellationToken);
        TempData["Success"] = "Rol ataması güncellendi";
        return RedirectToAction(nameof(Index));
    }
}
