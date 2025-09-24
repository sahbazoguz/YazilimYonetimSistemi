using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    private readonly IWebHostEnvironment _webHostEnvironment;

    private const long GuideMaxFileSizeBytes = 20 * 1024 * 1024;
    private const string GuideUploadRequestPath = "/uploads/kilavuzlar";

    public RequestsController(
        IRequestWorkflowService requestWorkflowService,
        IUserContextService userContextService,
        IDepartmentService departmentService,
        ISoftwareCatalogService catalogService,
        IWebHostEnvironment webHostEnvironment)
    {
        _requestWorkflowService = requestWorkflowService;
        _userContextService = userContextService;
        _departmentService = departmentService;
        _catalogService = catalogService;
        _webHostEnvironment = webHostEnvironment;
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

    [Authorize(Policy = RoleConstants.Policies.RequireDegerlendirici)]
    public async Task<IActionResult> DegerlendirmeBekleyen(CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null || !TryGetAssessmentStage(user.Role, out var stage) || stage == AssessmentStage.BaskanOnayi)
        {
            return Forbid();
        }

        var viewModel = await _requestWorkflowService.GetPendingAssessmentsAsync(stage, cancellationToken);
        return View("Liste", viewModel);
    }

    [Authorize(Policy = RoleConstants.Policies.RequireBaskan)]
    public async Task<IActionResult> BaskanOnayBekleyen(CancellationToken cancellationToken)
    {
        var viewModel = await _requestWorkflowService.GetPendingBaskanApprovalsAsync(cancellationToken);
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

        try
        {
            await _requestWorkflowService.ApproveAsync(id, user.Id, aciklama, cancellationToken);
            TempData["Success"] = "Talep onaya gönderildi";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
        }

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

        try
        {
            await _requestWorkflowService.RejectAsync(id, user.Id, aciklama, cancellationToken);
            TempData["Warning"] = "Talep reddedildi";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Detay), new { id });
    }

    public async Task<IActionResult> Degerlendir(int id, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null || !TryGetAssessmentStage(user.Role, out var stage))
        {
            return Forbid();
        }

        var detail = await _requestWorkflowService.GetDetailAsync(id, cancellationToken);
        if (detail?.Talep is null)
        {
            return NotFound();
        }

        if (!IsStageAllowed(detail.Talep.Status, stage))
        {
            TempData["Warning"] = "Talep mevcut aşamada değerlendirilemez.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var softwares = await _catalogService.GetCatalogAsync(null, null, null, cancellationToken);
        var input = new RequestAssessmentInputModel
        {
            RequestId = id,
            Stage = stage,
            Yazilimlar = softwares.Yazilimlar
        };

        var existing = detail.Degerlendirmeler.FirstOrDefault(a => a.Stage == stage);
        if (existing is not null)
        {
            input.Result = existing.Result;
            input.Notes = existing.Notes;
            input.ExistingSoftwareId = existing.ExistingSoftwareId;
        }

        ViewData["Talep"] = detail.Talep;
        ViewData["Asama"] = GetStageDisplayName(stage);
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Degerlendir(RequestAssessmentInputModel model, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null || !TryGetAssessmentStage(user.Role, out var stage))
        {
            return Forbid();
        }

        model.Stage = stage;

        if (!ModelState.IsValid)
        {
            var softwares = await _catalogService.GetCatalogAsync(null, null, null, cancellationToken);
            model.Yazilimlar = softwares.Yazilimlar;
            var detail = await _requestWorkflowService.GetDetailAsync(model.RequestId, cancellationToken);
            if (detail?.Talep is not null)
            {
                ViewData["Talep"] = detail.Talep;
            }
            ViewData["Asama"] = GetStageDisplayName(stage);
            return View(model);
        }

        try
        {
            await _requestWorkflowService.AssessAsync(model, user.Id, user.Role, cancellationToken);
            TempData["Success"] = stage == AssessmentStage.BaskanOnayi
                ? "Başkan kararı kaydedildi"
                : "Teknik değerlendirme kaydedildi";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Detay), new { id = model.RequestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = RoleConstants.Policies.RequireYazilimci)]
    public async Task<IActionResult> AlgoritmaGuncelle(int id, string algoritma, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        try
        {
            await _requestWorkflowService.UpdateAlgorithmAsync(id, user.Id, algoritma, cancellationToken);
            TempData["Success"] = "Algoritma taslağı güncellendi";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = RoleConstants.Policies.RequireYazilimci)]
    public async Task<IActionResult> KilavuzGuncelle(int id, IFormFile? teknikKilavuzDosyasi, IFormFile? kullanimKilavuzuDosyasi, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        if ((teknikKilavuzDosyasi is null || teknikKilavuzDosyasi.Length == 0) &&
            (kullanimKilavuzuDosyasi is null || kullanimKilavuzuDosyasi.Length == 0))
        {
            TempData["Warning"] = "Lütfen en az bir PDF dosyası seçin.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        string? teknikDosyaYolu = null;
        string? kullanimDosyaYolu = null;

        try
        {
            teknikDosyaYolu = await KaydetKilavuzAsync(teknikKilavuzDosyasi, cancellationToken);
            kullanimDosyaYolu = await KaydetKilavuzAsync(kullanimKilavuzuDosyasi, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            SilKlavuzDosyasi(teknikDosyaYolu);
            SilKlavuzDosyasi(kullanimDosyaYolu);
            TempData["Warning"] = ex.Message;
            return RedirectToAction(nameof(Detay), new { id });
        }

        try
        {
            await _requestWorkflowService.UpdateGuidesAsync(id, user.Id, teknikDosyaYolu, kullanimDosyaYolu, cancellationToken);
            TempData["Success"] = "Kılavuz bilgileri kaydedildi";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
            SilKlavuzDosyasi(teknikDosyaYolu);
            SilKlavuzDosyasi(kullanimDosyaYolu);
        }

        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = RoleConstants.Policies.RequireYazilimci)]
    public async Task<IActionResult> GelistirmeTamamla(int id, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        try
        {
            await _requestWorkflowService.MarkDevelopmentCompletedAsync(id, user.Id, cancellationToken);
            TempData["Success"] = "Geliştirme tamamlandı";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = RoleConstants.Policies.RequireBirimKullanicisi)]
    public async Task<IActionResult> TestiOnayla(int id, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        try
        {
            await _requestWorkflowService.ConfirmTestingAsync(id, user.Id, cancellationToken);
            TempData["Success"] = "Birim test onayı kaydedildi";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MesajGonder(int id, string mesaj, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(mesaj))
        {
            TempData["Warning"] = "Mesaj boş olamaz";
            return RedirectToAction(nameof(Detay), new { id });
        }

        try
        {
            await _requestWorkflowService.AddDiscussionMessageAsync(id, user.Id, mesaj, cancellationToken);
            TempData["Success"] = "Mesaj gönderildi";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
        }

        return RedirectToAction(nameof(Detay), new { id });
    }

    private async Task<string?> KaydetKilavuzAsync(IFormFile? dosya, CancellationToken cancellationToken)
    {
        if (dosya is null || dosya.Length == 0)
        {
            return null;
        }

        if (dosya.Length > GuideMaxFileSizeBytes)
        {
            throw new InvalidOperationException("Yüklenen dosya 20 MB sınırını aşamaz.");
        }

        var extension = Path.GetExtension(dosya.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Sadece PDF dosyaları yükleyebilirsiniz.");
        }

        var webRoot = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            throw new InvalidOperationException("Dosya yükleme dizini bulunamadı.");
        }

        var guideFolderPath = Path.Combine(webRoot, "uploads", "kilavuzlar");
        Directory.CreateDirectory(guideFolderPath);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(guideFolderPath, fileName);

        try
        {
            await using var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await dosya.CopyToAsync(stream, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("Dosya kaydedilirken bir hata oluştu.", ex);
        }

        return $"{GuideUploadRequestPath}/{fileName}";
    }

    private void SilKlavuzDosyasi(string? requestPath)
    {
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return;
        }

        var webRoot = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            return;
        }

        if (!requestPath.StartsWith(GuideUploadRequestPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var dosyaAdi = requestPath[GuideUploadRequestPath.Length..].TrimStart('/', '\\');
        if (string.IsNullOrWhiteSpace(dosyaAdi))
        {
            return;
        }

        var fizikselYol = Path.Combine(webRoot, "uploads", "kilavuzlar", dosyaAdi);
        if (System.IO.File.Exists(fizikselYol))
        {
            try
            {
                System.IO.File.Delete(fizikselYol);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // yoksay: temizlik denemesi başarısızsa kritik değildir
            }
        }
    }

    private static bool TryGetAssessmentStage(UserRole role, out AssessmentStage stage)
    {
        switch (role)
        {
            case UserRole.DegerlendiriciBir:
                stage = AssessmentStage.DegerlendiriciBir;
                return true;
            case UserRole.DegerlendiriciIki:
                stage = AssessmentStage.DegerlendiriciIki;
                return true;
            case UserRole.DegerlendiriciUc:
                stage = AssessmentStage.DegerlendiriciUc;
                return true;
            case UserRole.DegerlendirmeBaskani:
                stage = AssessmentStage.BaskanOnayi;
                return true;
            default:
                stage = default;
                return false;
        }
    }

    private static string GetStageDisplayName(AssessmentStage stage) => stage switch
    {
        AssessmentStage.DegerlendiriciBir => "Değerlendirici 1",
        AssessmentStage.DegerlendiriciIki => "Değerlendirici 2",
        AssessmentStage.DegerlendiriciUc => "Değerlendirici 3",
        AssessmentStage.BaskanOnayi => "Değerlendirme Başkanı",
        _ => "Değerlendirme"
    };

    private static bool IsStageAllowed(RequestStatus status, AssessmentStage stage) => stage switch
    {
        AssessmentStage.BaskanOnayi => status == RequestStatus.BaskanOnayiBekliyor,
        _ => status == RequestStatus.Degerlendirmede
    };
}
