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
public class RequestsController : Controller
{
    private readonly IRequestWorkflowService _requestWorkflowService;
    private readonly IUserContextService _userContextService;
    private readonly IDepartmentService _departmentService;
    private readonly ISoftwareCatalogService _catalogService;

    public RequestsController(
        IRequestWorkflowService requestWorkflowService,
        IUserContextService userContextService,
        IDepartmentService departmentService,
        ISoftwareCatalogService catalogService)
    {
        _requestWorkflowService = requestWorkflowService;
        _userContextService = userContextService;
        _departmentService = departmentService;
        _catalogService = catalogService;
    }

    [Authorize(Policy = RoleConstants.Policies.RequireBirimKullanicisi)]
    public async Task<IActionResult> Taleplerim(CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        var viewModel = await _requestWorkflowService.GetRequestsForUserAsync(user.Id, cancellationToken);
        return View("Liste", viewModel);
    }

    [Authorize(Policy = RoleConstants.Policies.RequireBirimYetkilisi)]
    public async Task<IActionResult> Birim(CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user?.DepartmentId is null)
        {
            return Forbid();
        }

        var viewModel = await _requestWorkflowService.GetRequestsForDepartmentAsync(user.DepartmentId.Value, cancellationToken);
        return View("Liste", viewModel);
    }

    [Authorize(Policy = RoleConstants.Policies.RequireBirimYetkilisi)]
    public async Task<IActionResult> OnayBekleyen(CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user?.DepartmentId is null)
        {
            return Forbid();
        }

        var viewModel = await _requestWorkflowService.GetPendingApprovalsAsync(user.DepartmentId.Value, cancellationToken);
        return View("Liste", viewModel);
    }

    [Authorize(Policy = RoleConstants.Policies.RequireItTeam)]
    public async Task<IActionResult> DegerlendirmeBekleyen(CancellationToken cancellationToken)
    {
        var viewModel = await _requestWorkflowService.GetPendingAssessmentsAsync(cancellationToken);
        return View("Liste", viewModel);
    }

    [Authorize(Policy = RoleConstants.Policies.RequireBirimKullanicisi)]
    public async Task<IActionResult> Olustur(CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetDepartmentsAsync(cancellationToken);
        var viewModel = new RequestCreateViewModel
        {
            Departments = departments
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Policy = RoleConstants.Policies.RequireBirimKullanicisi)]
    public async Task<IActionResult> Olustur(RequestCreateViewModel viewModel, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            viewModel.Departments = await _departmentService.GetDepartmentsAsync(cancellationToken);
            return View(viewModel);
        }

        var requestId = await _requestWorkflowService.CreateRequestAsync(viewModel, user.Id, cancellationToken);
        TempData["Success"] = "Talep başarıyla oluşturuldu";
        return RedirectToAction(nameof(Detay), new { id = requestId });
    }

    public async Task<IActionResult> Detay(int id, CancellationToken cancellationToken)
    {
        var detail = await _requestWorkflowService.GetDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = RoleConstants.Policies.RequireBirimYetkilisi)]
    public async Task<IActionResult> Onayla(int id, string? aciklama, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        await _requestWorkflowService.ApproveAsync(id, user.Id, aciklama, cancellationToken);
        TempData["Success"] = "Talep onaya gönderildi";
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = RoleConstants.Policies.RequireBirimYetkilisi)]
    public async Task<IActionResult> Reddet(int id, string? aciklama, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        await _requestWorkflowService.RejectAsync(id, user.Id, aciklama, cancellationToken);
        TempData["Warning"] = "Talep reddedildi";
        return RedirectToAction(nameof(Detay), new { id });
    }

    [Authorize(Policy = RoleConstants.Policies.RequireItTeam)]
    public async Task<IActionResult> Degerlendir(int id, CancellationToken cancellationToken)
    {
        var detail = await _requestWorkflowService.GetDetailAsync(id, cancellationToken);
        if (detail?.Talep is null)
        {
            return NotFound();
        }

        var softwares = await _catalogService.GetCatalogAsync(null, null, null, cancellationToken);
        var input = new RequestAssessmentInputModel
        {
            RequestId = id,
            Yazilimlar = softwares.Yazilimlar
        };

        ViewData["Talep"] = detail.Talep;
        return View(input);
    }

    [HttpPost]
    [Authorize(Policy = RoleConstants.Policies.RequireItTeam)]
    public async Task<IActionResult> Degerlendir(RequestAssessmentInputModel model, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var softwares = await _catalogService.GetCatalogAsync(null, null, null, cancellationToken);
            model.Yazilimlar = softwares.Yazilimlar;
            return View(model);
        }

        await _requestWorkflowService.AssessAsync(model, user.Id, cancellationToken);
        TempData["Success"] = "Teknik değerlendirme kaydedildi";
        return RedirectToAction(nameof(Detay), new { id = model.RequestId });
    }
}
