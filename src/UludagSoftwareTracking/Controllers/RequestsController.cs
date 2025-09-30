using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly ISoftwareCatalogService _catalogService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    private const long GuideMaxFileSizeBytes = 20 * 1024 * 1024;
    private const string GuideUploadRequestPath = "/uploads/kilavuzlar";

    public RequestsController(
        IRequestWorkflowService requestWorkflowService,
        IUserContextService userContextService,
        ISoftwareCatalogService catalogService,
        IWebHostEnvironment webHostEnvironment)
    {
        _requestWorkflowService = requestWorkflowService;
        _userContextService = userContextService;
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
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        if (user.Role == UserRole.BirimYetkilisi)
        {
            TempData["Warning"] = "Birim yetkilileri yeni talep oluşturamaz.";
            return RedirectToAction(nameof(Birim));
        }

        if (user.DepartmentId is null)
        {
            TempData["Warning"] = "Birim bilgisi bulunamadı. Lütfen sistem yöneticisine başvurun.";
            return RedirectToAction(nameof(Taleplerim));
        }

        var viewModel = new RequestCreateViewModel
        {
            DepartmentId = user.DepartmentId.Value,
            DepartmentName = user.Department?.Name ?? ""
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

        if (user.Role == UserRole.BirimYetkilisi)
        {
            TempData["Warning"] = "Birim yetkilileri yeni talep oluşturamaz.";
            return RedirectToAction(nameof(Birim));
        }

        if (user.DepartmentId is null)
        {
            TempData["Warning"] = "Birim bilgileriniz eksik olduğu için talep oluşturamazsınız.";
            return RedirectToAction(nameof(Taleplerim));
        }

        viewModel.DepartmentId = user.DepartmentId.Value;
        viewModel.DepartmentName = user.Department?.Name ?? string.Empty;

        if (!ModelState.IsValid)
        {
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

        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is not null)
        {
            if (user.Role == UserRole.BirimKullanicisi || user.Role == UserRole.BirimYetkilisi)
            {
                if (detail.Talep?.DepartmentId != user.DepartmentId)
                {
                    return Forbid();
                }

                detail = new RequestDetailViewModel
                {
                    Talep = detail.Talep,
                    Onaylar = detail.Onaylar,
                    Degerlendirmeler = Array.Empty<RequestAssessment>(),
                    Proje = detail.Proje,
                    Mesajlar = detail.Mesajlar,
                    Workflows = detail.Workflows
                };
            }
            else if (user.Role == UserRole.Personel || user.Role == UserRole.Ogrenci)
            {
                return Forbid();
            }
            else if (user.Role == UserRole.Yazilimci)
            {
                var request = detail.Talep;
                var statusAllowed = request is not null && (request.Status == RequestStatus.Gelistirmede
                    || request.Status == RequestStatus.Tamamlandi
                    || request.Status == RequestStatus.Kapandi);
                var assigned = detail.Proje?.Assignments.Any(a => a.UserId == user.Id) == true;

                if (!statusAllowed || !assigned)
                {
                    return Forbid();
                }
            }
        }

        var workflowLocked = detail.Talep?.Status != RequestStatus.Gelistirmede
            && detail.Talep?.Status != RequestStatus.Tamamlandi
            && detail.Talep?.Status != RequestStatus.Kapandi;

        ViewData["IsAkisiPasif"] = workflowLocked;
        ViewData["TakimAtamaIzni"] = user?.Role == UserRole.DegerlendirmeBaskani
            && detail.Talep?.Status == RequestStatus.Gelistirmede;

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

    [Authorize(Policy = RoleConstants.Policies.RequireBaskan)]
    public async Task<IActionResult> Gorevlendir(int id, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _requestWorkflowService.GetProjectTeamAsync(id, cancellationToken);
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
            return RedirectToAction(nameof(Detay), new { id });
        }
    }

    [HttpPost]
    [Authorize(Policy = RoleConstants.Policies.RequireBaskan)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gorevlendir(ProjectTeamAssignmentInputModel model, CancellationToken cancellationToken)
    {
        model.DeveloperIds ??= new List<int>();
        model.TesterIds ??= new List<int>();

        if (model.DeveloperIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.DeveloperIds), "En az bir geliştirici seçmelisiniz.");
        }

        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var refreshed = await _requestWorkflowService.GetProjectTeamAsync(model.RequestId, cancellationToken);
            refreshed.LeadUserId = model.LeadUserId;
            refreshed.DeveloperIds = model.DeveloperIds;
            refreshed.TesterIds = model.TesterIds;
            return View(refreshed);
        }

        try
        {
            await _requestWorkflowService.UpdateProjectTeamAsync(model, user.Id, cancellationToken);
            TempData["Success"] = "Proje ekibi güncellendi";
            return RedirectToAction(nameof(Detay), new { id = model.RequestId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
            ModelState.AddModelError(string.Empty, ex.Message);
            var refreshed = await _requestWorkflowService.GetProjectTeamAsync(model.RequestId, cancellationToken);
            refreshed.LeadUserId = model.LeadUserId;
            refreshed.DeveloperIds = model.DeveloperIds;
            refreshed.TesterIds = model.TesterIds;
            return View(refreshed);
        }
    }

    [Authorize(Policy = RoleConstants.Policies.RequireDegerlendirici)]
    public async Task<IActionResult> DegerlendirmeGecmisi(DateTime? baslangic, DateTime? bitis, AssessmentResult? karar, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        var model = await _requestWorkflowService.GetAssessmentHistoryAsync(user.Id, baslangic, bitis, karar, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = RoleConstants.Policies.RequireWorkflowEditor)]
    public async Task<IActionResult> IsAkisiGuncelle(int id, string isAkisiJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(isAkisiJson))
        {
            TempData["Warning"] = "İş akışı verisi gönderilmedi.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            var payload = JsonSerializer.Deserialize<WorkflowEditorPostModel>(isAkisiJson, options);
            if (payload is null)
            {
                TempData["Warning"] = "İş akışı verisi çözümlenemedi.";
            }
            else
            {
                await _requestWorkflowService.UpdateWorkflowsAsync(id, user.Id, payload, cancellationToken);
                TempData["Success"] = "İş akışları güncellendi";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["Warning"] = ex.Message;
        }
        catch (JsonException)
        {
            TempData["Warning"] = "İş akışı verisi çözümlenemedi.";
        }

        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KilavuzGuncelle(int id, IFormFile? teknikKilavuzDosyasi, IFormFile? kullanimKilavuzuDosyasi, CancellationToken cancellationToken)
    {
        var user = await _userContextService.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Forbid();
        }

        var isDeveloper = user.Role == UserRole.Yazilimci;
        var isUnitUser = user.Role == UserRole.BirimKullanicisi;

        if (!isDeveloper && !isUnitUser)
        {
            TempData["Warning"] = "Kılavuz yükleme yetkiniz yok.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var hasTechnical = teknikKilavuzDosyasi is not null && teknikKilavuzDosyasi.Length > 0;
        var hasUserManual = kullanimKilavuzuDosyasi is not null && kullanimKilavuzuDosyasi.Length > 0;

        if (!hasTechnical && !hasUserManual)
        {
            TempData["Warning"] = "Lütfen en az bir PDF dosyası seçin.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        if (hasTechnical && !isDeveloper)
        {
            TempData["Warning"] = "Teknik kılavuz yükleme yetkiniz yok.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        if (hasUserManual && !isUnitUser)
        {
            TempData["Warning"] = "Kullanım kılavuzu yükleme yetkiniz yok.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var successMessages = new List<string>();

        if (hasTechnical)
        {
            string? teknikDosyaYolu = null;
            try
            {
                teknikDosyaYolu = await KaydetKilavuzAsync(teknikKilavuzDosyasi, cancellationToken);
                if (!string.IsNullOrWhiteSpace(teknikDosyaYolu))
                {
                    await _requestWorkflowService.UpdateTechnicalGuideAsync(id, user.Id, teknikDosyaYolu, cancellationToken);
                    successMessages.Add("Teknik kılavuz güncellendi");
                }
            }
            catch (InvalidOperationException ex)
            {
                SilKlavuzDosyasi(teknikDosyaYolu);
                TempData["Warning"] = ex.Message;
                return RedirectToAction(nameof(Detay), new { id });
            }
        }

        if (hasUserManual)
        {
            string? kullanimDosyaYolu = null;
            try
            {
                kullanimDosyaYolu = await KaydetKilavuzAsync(kullanimKilavuzuDosyasi, cancellationToken);
                if (!string.IsNullOrWhiteSpace(kullanimDosyaYolu))
                {
                    await _requestWorkflowService.UpdateUserGuideAsync(id, user.Id, kullanimDosyaYolu, cancellationToken);
                    successMessages.Add("Kullanım kılavuzu güncellendi");
                }
            }
            catch (InvalidOperationException ex)
            {
                SilKlavuzDosyasi(kullanimDosyaYolu);
                TempData["Warning"] = ex.Message;
                return RedirectToAction(nameof(Detay), new { id });
            }
        }

        if (successMessages.Count > 0)
        {
            TempData["Success"] = string.Join(" ", successMessages);
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
