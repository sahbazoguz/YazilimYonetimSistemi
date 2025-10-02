using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UludagSoftwareTracking.Data;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Models.ViewModels;
using UludagSoftwareTracking.Services.Interfaces;
using UludagSoftwareTracking.Services.Security;

namespace UludagSoftwareTracking.Services.Implementations;

public class RequestWorkflowService : IRequestWorkflowService
{
    private static readonly AssessmentStage[] RequiredEvaluatorStages =
    {
        AssessmentStage.DegerlendiriciBir,
        AssessmentStage.DegerlendiriciIki,
        AssessmentStage.DegerlendiriciUc
    };

    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;

    public RequestWorkflowService(ApplicationDbContext context, IAuditLogService auditLogService, INotificationService notificationService)
    {
        _context = context;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
    }

    public async Task<RequestOverviewViewModel> GetRequestsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var requests = await _context.SoftwareRequests
            .Where(r => r.RequestedByUserId == userId)
            .Include(r => r.Department)
            .Include(r => r.RequestedByUser)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return CreateOverview("Taleplerim", requests);
    }

    public async Task<RequestOverviewViewModel> GetRequestsForDepartmentAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var requests = await _context.SoftwareRequests
            .Where(r => r.DepartmentId == departmentId)
            .Include(r => r.Department)
            .Include(r => r.RequestedByUser)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return CreateOverview("Birim Talepleri", requests);
    }

    public async Task<RequestOverviewViewModel> GetPendingApprovalsAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var requests = await _context.SoftwareRequests
            .Where(r => r.DepartmentId == departmentId && r.Status == RequestStatus.OnayBekleniyor)
            .Include(r => r.Department)
            .Include(r => r.RequestedByUser)
            .OrderBy(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return CreateOverview("Onay Bekleyen Talepler", requests);
    }

    public async Task<RequestOverviewViewModel> GetPendingAssessmentsAsync(AssessmentStage stage, CancellationToken cancellationToken = default)
    {
        var requests = await _context.SoftwareRequests
            .Where(r => r.Status == RequestStatus.Degerlendirmede)
            .Include(r => r.Assessments)
            .Include(r => r.Department)
            .Include(r => r.RequestedByUser)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var filtered = requests
            .Where(r => !r.Assessments.Any(a => a.Stage == stage && a.Result != AssessmentResult.Beklemede))
            .OrderBy(r => r.CreatedAt)
            .ToList();

        return CreateOverview("Değerlendirme Bekleyen Talepler", filtered);
    }

    public async Task<RequestOverviewViewModel> GetPendingBaskanApprovalsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _context.SoftwareRequests
            .Where(r => r.Status == RequestStatus.BaskanOnayiBekliyor)
            .Include(r => r.Department)
            .Include(r => r.RequestedByUser)
            .OrderBy(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return CreateOverview("Başkan Onayı Bekleyen Talepler", requests);
    }

    public async Task<SoftwareRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.SoftwareRequests
            .Include(r => r.Department)
            .Include(r => r.RequestedByUser)
            .Include(r => r.Approvals)
            .ThenInclude(a => a.ApprovedByUser)
            .Include(r => r.Assessments)
            .ThenInclude(a => a.AssessedByUser)
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .ThenInclude(a => a.User)
            .Include(r => r.Project)
            .ThenInclude(p => p!.LeadUser)
            .Include(r => r.Project)
            .ThenInclude(p => p!.WorkflowDefinitions)
            .ThenInclude(w => w.Steps)
            .Include(r => r.DiscussionMessages)
            .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<RequestDetailViewModel?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var request = await GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return null;
        }

        return new RequestDetailViewModel
        {
            Talep = request,
            Onaylar = request.Approvals
                .OrderByDescending(a => a.DecidedAt ?? request.CreatedAt)
                .ToArray(),
            Degerlendirmeler = request.Assessments
                .OrderBy(a => a.Stage)
                .ThenByDescending(a => a.AssessedOn ?? request.CreatedAt)
                .ToArray(),
            Proje = request.Project,
            Mesajlar = request.DiscussionMessages
                .OrderBy(m => m.PostedOn)
                .ToArray(),
            Workflows = BuildWorkflowViewModels(request.Project)
        };
    }

    public async Task<ProjectTeamAssignmentInputModel> GetProjectTeamAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Project is null)
        {
            throw new InvalidOperationException("Başkan onayı verilmeden ekip görevlendirilemez.");
        }

        if (request.Status != RequestStatus.Gelistirmede && request.Status != RequestStatus.Tamamlandi)
        {
            throw new InvalidOperationException("Ekip görevlendirmesi yalnızca başkan onayı sonrası geliştirme aşamasında yapılabilir.");
        }

        var candidateUsers = await _context.UserProfiles
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Role == UserRole.Yazilimci)
            .OrderBy(u => u.FullName ?? u.UserName)
            .Select(u => new SelectableUserViewModel
            {
                Id = u.Id,
                AdSoyad = u.FullName ?? u.UserName ?? $"Kullanıcı {u.Id}",
                Birim = u.Department != null ? u.Department.Name : "-"
            })
            .ToListAsync(cancellationToken);

        var developerIds = request.Project.Assignments
            .Where(a => a.AssignedRole == RoleConstants.Roles.Yazilimci || a.AssignedRole == RoleConstants.Roles.EkipLideri)
            .Select(a => a.UserId)
            .Where(id => id != 0)
            .Distinct()
            .ToList();

        var testerIds = request.Project.Assignments
            .Where(a => a.AssignedRole == RoleConstants.Roles.TestYazilimcisi)
            .Select(a => a.UserId)
            .Where(id => id != 0)
            .Distinct()
            .ToList();

        return new ProjectTeamAssignmentInputModel
        {
            RequestId = request.Id,
            TalepBasligi = request.Title,
            LeadUserId = request.Project.LeadUserId,
            DeveloperIds = developerIds,
            TesterIds = testerIds,
            Adaylar = candidateUsers
        };
    }

    public async Task<int> CreateRequestAsync(RequestCreateViewModel model, int userId, CancellationToken cancellationToken = default)
    {
        var request = new SoftwareRequest
        {
            Title = model.Title,
            Description = model.Description,
            Priority = model.Priority,
            DesiredCompletionDate = model.DesiredCompletionDate,
            DepartmentId = model.DepartmentId,
            RequestedByUserId = userId,
            Status = RequestStatus.OnayBekleniyor,
            CreatedAt = DateTime.UtcNow
        };

        await _context.SoftwareRequests.AddAsync(request, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var approver = await _context.UserProfiles
            .Where(u => u.DepartmentId == model.DepartmentId && u.Role == UserRole.BirimYetkilisi)
            .FirstOrDefaultAsync(cancellationToken);

        var approval = new RequestApproval
        {
            RequestId = request.Id,
            ApprovedByUserId = approver?.Id,
            Status = ApprovalStatus.Beklemede
        };

        await _context.RequestApprovals.AddAsync(approval, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(userId, "Talep Oluşturma", nameof(SoftwareRequest), request.Id,
            $"Talep Başlığı: {request.Title}", cancellationToken);

        return request.Id;
    }

    public async Task ApproveAsync(int requestId, int approverUserId, string? notes, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Approvals)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Status != RequestStatus.OnayBekleniyor)
        {
            throw new InvalidOperationException("Talep onay aşamasında değil.");
        }

        var approver = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Id == approverUserId, cancellationToken)
            ?? throw new InvalidOperationException("Onaylayacak kullanıcı bulunamadı");

        if (approver.DepartmentId != request.DepartmentId)
        {
            throw new InvalidOperationException("Yalnızca talebin ait olduğu birim onay verebilir.");
        }

        request.Status = RequestStatus.Degerlendirmede;
        request.UpdatedAt = DateTime.UtcNow;

        var approval = request.Approvals.FirstOrDefault();
        if (approval is null)
        {
            approval = new RequestApproval
            {
                RequestId = requestId
            };
            request.Approvals.Add(approval);
        }

        approval.ApprovedByUserId = approverUserId;
        approval.DecidedAt = DateTime.UtcNow;
        approval.Status = ApprovalStatus.Onaylandi;
        approval.Notes = notes;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(approverUserId, "Talep Onayı", nameof(SoftwareRequest), request.Id,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(), cancellationToken);

        var recipients = new List<UserProfile>();
        if (request.RequestedByUser is not null)
        {
            recipients.Add(request.RequestedByUser);
        }

        var evaluators = await GetUsersByRolesAsync(cancellationToken,
            UserRole.DegerlendiriciBir,
            UserRole.DegerlendiriciIki,
            UserRole.DegerlendiriciUc,
            UserRole.DegerlendirmeBaskani);
        recipients.AddRange(evaluators);

        await _notificationService.SendAsync(recipients,
            "Talep Onaylandı",
            $"\"{request.Title}\" talebi değerlendirme aşamasına geçti.",
            $"/Requests/Detay/{request.Id}",
            cancellationToken);
    }

    public async Task RejectAsync(int requestId, int approverUserId, string? notes, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Approvals)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Status != RequestStatus.OnayBekleniyor)
        {
            throw new InvalidOperationException("Talep onay aşamasında değil.");
        }

        var approver = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Id == approverUserId, cancellationToken)
            ?? throw new InvalidOperationException("Onaylayacak kullanıcı bulunamadı");

        if (approver.DepartmentId != request.DepartmentId)
        {
            throw new InvalidOperationException("Yalnızca talebin ait olduğu birim işlem yapabilir.");
        }

        request.Status = RequestStatus.Reddedildi;
        request.UpdatedAt = DateTime.UtcNow;
        request.RejectionReason = notes;

        var approval = request.Approvals.FirstOrDefault();
        if (approval is null)
        {
            approval = new RequestApproval { RequestId = requestId };
            request.Approvals.Add(approval);
        }

        approval.ApprovedByUserId = approverUserId;
        approval.DecidedAt = DateTime.UtcNow;
        approval.Status = ApprovalStatus.Reddedildi;
        approval.Notes = notes;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(approverUserId, "Talep Reddi", nameof(SoftwareRequest), request.Id,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(), cancellationToken);

        var recipients = new List<UserProfile>();
        if (request.RequestedByUser is not null)
        {
            recipients.Add(request.RequestedByUser);
        }

        await _notificationService.SendAsync(recipients,
            "Talep Reddedildi",
            $"\"{request.Title}\" talebi reddedildi.",
            $"/Requests/Detay/{request.Id}",
            cancellationToken);
    }

    public async Task AssessAsync(RequestAssessmentInputModel model, int assessorUserId, UserRole assessorRole, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Assessments)
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .FirstOrDefaultAsync(r => r.Id == model.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        var stage = MapStage(assessorRole);

        if (stage == AssessmentStage.BaskanOnayi)
        {
            if (request.Status != RequestStatus.BaskanOnayiBekliyor)
            {
                throw new InvalidOperationException("Talep başkan onayına hazır değil.");
            }

            if (!RequiredEvaluatorStages.All(s => request.Assessments.Any(a => a.Stage == s && a.Result != AssessmentResult.Beklemede)))
            {
                throw new InvalidOperationException("Başkan onayı için tüm değerlendirme raporlarının tamamlanması gerekir.");
            }
        }
        else if (request.Status != RequestStatus.Degerlendirmede)
        {
            throw new InvalidOperationException("Talep değerlendirme aşamasında değil.");
        }

        var assessment = request.Assessments.FirstOrDefault(a => a.Stage == stage);
        if (assessment is null)
        {
            assessment = new RequestAssessment
            {
                RequestId = request.Id,
                Stage = stage
            };
            request.Assessments.Add(assessment);
        }

        assessment.AssessedByUserId = assessorUserId;
        assessment.AssessedOn = DateTime.UtcNow;
        assessment.Result = model.Result;
        assessment.Notes = model.Notes;
        assessment.ExistingSoftwareId = model.ExistingSoftwareId;

        if (stage == AssessmentStage.BaskanOnayi)
        {
            switch (model.Result)
            {
                case AssessmentResult.Uygun:
                case AssessmentResult.YeniGelistirme:
                    request.Status = RequestStatus.Gelistirmede;
                    await EnsureProjectAsync(request, cancellationToken);
                    if (request.Project is not null)
                    {
                        request.Project.Status = ProjectStatus.Gelistirme;
                    }
                    break;
                case AssessmentResult.VarOlanYazilimaYonlendirildi:
                    request.Status = RequestStatus.Kapandi;
                    break;
                case AssessmentResult.UygunDegil:
                    request.Status = RequestStatus.Reddedildi;
                    request.RejectionReason = model.Notes;
                    break;
                default:
                    request.Status = RequestStatus.BaskanOnayiBekliyor;
                    break;
            }
        }
        else if (RequiredEvaluatorStages.All(s => request.Assessments.Any(a => a.Stage == s && a.Result != AssessmentResult.Beklemede)))
        {
            request.Status = RequestStatus.BaskanOnayiBekliyor;
        }

        request.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(assessorUserId, stage == AssessmentStage.BaskanOnayi ? "Başkan Kararı" : "Teknik Değerlendirme",
            nameof(SoftwareRequest), request.Id,
            $"Aşama: {stage} Sonuç: {model.Result}", cancellationToken);
    }

    public async Task UpdateWorkflowsAsync(int requestId, int userId, WorkflowEditorPostModel model, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .Include(r => r.Project)
            .ThenInclude(p => p!.WorkflowDefinitions)
            .ThenInclude(w => w.Steps)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Project is null)
        {
            throw new InvalidOperationException("Proje oluşturulmadan iş akışı güncellenemez.");
        }

        if (request.Status != RequestStatus.Gelistirmede && request.Status != RequestStatus.Tamamlandi && request.Status != RequestStatus.Kapandi)
        {
            throw new InvalidOperationException("Başkan onayı tamamlanmadan iş akışı güncellenemez.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        var isDeveloper = IsDeveloper(user);
        var isAdmin = user.Role == UserRole.Admin;
        var isUnitUser = user.Role == UserRole.BirimKullanicisi;

        if (!isDeveloper && !isAdmin && !isUnitUser)
        {
            throw new InvalidOperationException("İş akışı üzerinde değişiklik yapma yetkiniz yok.");
        }

        if (isDeveloper && !request.Project.Assignments.Any(a => a.UserId == userId))
        {
            throw new InvalidOperationException("Bu projeye atanmadınız.");
        }

        if (isUnitUser && user.DepartmentId != request.DepartmentId)
        {
            throw new InvalidOperationException("Yalnızca talep sahibi birim iş akışını güncelleyebilir.");
        }

        var sanitizedWorkflows = NormalizeWorkflowPayload(model);

        ApplyWorkflowUpdates(request.Project, sanitizedWorkflows);

        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(userId, "İş Akışı Güncelleme", nameof(Project), request.Project.Id,
            null, cancellationToken);
    }

    public async Task UpdateTechnicalGuideAsync(int requestId, int userId, string technicalGuidePath, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Project is null)
        {
            throw new InvalidOperationException("Proje oluşturulmadan kılavuz bilgileri güncellenemez.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (!IsDeveloper(user))
        {
            throw new InvalidOperationException("Kılavuz bilgilerini güncelleme yetkiniz yok.");
        }

        if (!request.Project.Assignments.Any(a => a.UserId == userId))
        {
            throw new InvalidOperationException("Bu projeye atanmadınız.");
        }

        request.Project.TechnicalGuidePath = string.IsNullOrWhiteSpace(technicalGuidePath)
            ? null
            : technicalGuidePath.Trim();
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var recipients = new List<UserProfile>();
        if (request.RequestedByUser is not null)
        {
            recipients.Add(request.RequestedByUser);
        }

        recipients.AddRange(await GetDepartmentRoleUsersAsync(request.DepartmentId, cancellationToken,
            UserRole.BirimYetkilisi, UserRole.BirimKullanicisi));
        recipients.AddRange(await GetProjectDevelopersAsync(request.Project.Id, cancellationToken));

        recipients = recipients
            .Where(u => u is not null && u.Id != userId)
            .GroupBy(u => u!.Id)
            .Select(g => g.First()!)
            .ToList();

        await _notificationService.SendAsync(recipients,
            "Teknik Kılavuz Güncellendi",
            $"\"{request.Title}\" talebinin teknik kılavuzu güncellendi.",
            request.Project.TechnicalGuidePath,
            cancellationToken);

        await _auditLogService.RecordAsync(userId, "Teknik Kılavuz Güncelleme", nameof(Project), request.Project.Id,
            request.Project.TechnicalGuidePath, cancellationToken);
    }

    public async Task UpdateUserGuideAsync(int requestId, int userId, string userGuidePath, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Project is null)
        {
            throw new InvalidOperationException("Proje oluşturulmadan kılavuz bilgileri güncellenemez.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (user.Role != UserRole.BirimKullanicisi)
        {
            throw new InvalidOperationException("Kılavuz bilgilerini güncelleme yetkiniz yok.");
        }

        if (user.DepartmentId != request.DepartmentId)
        {
            throw new InvalidOperationException("Yalnızca talep sahibi birim kullanım kılavuzu yükleyebilir.");
        }

        request.Project.UserGuidePath = string.IsNullOrWhiteSpace(userGuidePath)
            ? null
            : userGuidePath.Trim();
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var recipients = new List<UserProfile>();
        recipients.AddRange(await GetProjectDevelopersAsync(request.Project.Id, cancellationToken));
        recipients.AddRange(await GetDepartmentRoleUsersAsync(request.DepartmentId, cancellationToken, UserRole.BirimYetkilisi));

        if (request.RequestedByUser is not null)
        {
            recipients.Add(request.RequestedByUser);
        }

        recipients = recipients
            .Where(u => u is not null && u.Id != userId)
            .GroupBy(u => u!.Id)
            .Select(g => g.First()!)
            .ToList();

        await _notificationService.SendAsync(recipients,
            "Kullanım Kılavuzu Güncellendi",
            $"\"{request.Title}\" talebinin kullanım kılavuzu güncellendi.",
            request.Project.UserGuidePath,
            cancellationToken);

        await _auditLogService.RecordAsync(userId, "Kullanım Kılavuzu Güncelleme", nameof(Project), request.Project.Id,
            request.Project.UserGuidePath, cancellationToken);
    }

    public async Task MarkDevelopmentCompletedAsync(int requestId, int userId, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Status != RequestStatus.Gelistirmede)
        {
            throw new InvalidOperationException("Talep geliştirme aşamasında değil.");
        }

        if (request.Project is null)
        {
            throw new InvalidOperationException("Proje bulunamadı.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (!IsDeveloper(user) && user.Role != UserRole.Admin)
        {
            throw new InvalidOperationException("Bu işlemi yapma yetkiniz yok.");
        }

        if (IsDeveloper(user) && !request.Project.Assignments.Any(a => a.UserId == userId))
        {
            throw new InvalidOperationException("Bu projeye atanmadınız.");
        }

        request.Status = RequestStatus.Tamamlandi;
        request.UpdatedAt = DateTime.UtcNow;
        request.Project.Status = ProjectStatus.Test;
        request.Project.CompletedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var recipients = new List<UserProfile>();
        recipients.AddRange(await GetDepartmentRoleUsersAsync(request.DepartmentId, cancellationToken, UserRole.BirimKullanicisi, UserRole.BirimYetkilisi));

        await _notificationService.SendAsync(recipients,
            "Geliştirme Tamamlandı",
            $"\"{request.Title}\" talebi test aşamasına geçti.",
            $"/Requests/Detay/{request.Id}",
            cancellationToken);

        await _auditLogService.RecordAsync(userId, "Geliştirme Tamamlama", nameof(SoftwareRequest), request.Id, null, cancellationToken);
    }

    public async Task ConfirmTestingAsync(int requestId, int userId, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Department)
            .Include(r => r.RequestedByUser)
            .Include(r => r.Project)
                .ThenInclude(p => p!.Assignments)
            .Include(r => r.Project)
                .ThenInclude(p => p!.ReleasedSoftware)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Status != RequestStatus.Tamamlandi)
        {
            throw new InvalidOperationException("Talep test onayı için uygun durumda değil.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (user.Role != UserRole.Admin &&
            !(user.Role == UserRole.BirimKullanicisi || user.Role == UserRole.BirimYetkilisi) )
        {
            throw new InvalidOperationException("Test onayı sadece talep sahibi birim tarafından verilebilir.");
        }

        if (user.Role != UserRole.Admin && user.DepartmentId != request.DepartmentId)
        {
            throw new InvalidOperationException("Yalnızca talebi oluşturan birim test onayı verebilir.");
        }

        request.Status = RequestStatus.Kapandi;
        request.UpdatedAt = DateTime.UtcNow;
        if (request.Project is not null)
        {
            request.Project.Status = ProjectStatus.Tamamlandi;
            request.Project.TestConfirmedOn = DateTime.UtcNow;
            await EnsureReleasedSoftwareAsync(request, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var recipients = await GetProjectDevelopersAsync(request.Project?.Id ?? 0, cancellationToken);
        await _notificationService.SendAsync(recipients,
            "Test Onayı Verildi",
            $"\"{request.Title}\" talebi için birim testi tamamlandı.",
            $"/Requests/Detay/{request.Id}",
            cancellationToken);

        await _auditLogService.RecordAsync(userId, "Test Onayı", nameof(SoftwareRequest), request.Id, null, cancellationToken);
    }

    public async Task AddDiscussionMessageAsync(int requestId, int userId, string message, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .Include(r => r.DiscussionMessages)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Project is null)
        {
            throw new InvalidOperationException("Proje oluşturulmadan sohbet başlatılamaz.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (!IsDiscussionParticipant(user, request))
        {
            throw new InvalidOperationException("Sohbete mesaj gönderme yetkiniz yok.");
        }

        var trimmed = message?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("Mesaj içeriği boş olamaz.");
        }

        if (trimmed.Length > 1000)
        {
            trimmed = trimmed[..1000];
        }

        var discussionMessage = new RequestDiscussionMessage
        {
            RequestId = request.Id,
            SenderId = userId,
            Message = trimmed,
            PostedOn = DateTime.UtcNow
        };

        request.DiscussionMessages.Add(discussionMessage);
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var logOzet = trimmed.Length > 150 ? trimmed[..150] + "…" : trimmed;
        await _auditLogService.RecordAsync(userId, "Talep Mesajı", nameof(SoftwareRequest), request.Id,
            logOzet, cancellationToken);
    }

    public async Task UpdateProjectTeamAsync(ProjectTeamAssignmentInputModel model, int actingUserId, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .FirstOrDefaultAsync(r => r.Id == model.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Project is null)
        {
            throw new InvalidOperationException("Başkan onayı verilmeden ekip görevlendirilemez.");
        }

        if (request.Status != RequestStatus.Gelistirmede && request.Status != RequestStatus.Tamamlandi)
        {
            throw new InvalidOperationException("Talep geliştirme aşamasına geçmeden ekip atanamaz.");
        }

        var developerIds = model.DeveloperIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList() ?? new List<int>();

        if (model.LeadUserId.HasValue && !developerIds.Contains(model.LeadUserId.Value))
        {
            developerIds.Insert(0, model.LeadUserId.Value);
        }

        if (developerIds.Count == 0)
        {
            throw new InvalidOperationException("En az bir geliştirici seçmelisiniz.");
        }

        var testerIds = model.TesterIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList() ?? new List<int>();

        var selectedIds = developerIds.Concat(testerIds).Distinct().ToArray();
        var selectedUsers = await _context.UserProfiles
            .Where(u => u.IsActive && u.Role == UserRole.Yazilimci && selectedIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        if (!developerIds.All(id => selectedUsers.Any(u => u.Id == id)))
        {
            throw new InvalidOperationException("Seçilen geliştirici bilgileri geçersiz.");
        }

        testerIds = testerIds
            .Where(id => selectedUsers.Any(u => u.Id == id))
            .ToList();

        var relevantAssignments = request.Project.Assignments
            .Where(a => a.AssignedRole == RoleConstants.Roles.Yazilimci
                        || a.AssignedRole == RoleConstants.Roles.EkipLideri
                        || a.AssignedRole == RoleConstants.Roles.TestYazilimcisi)
            .ToList();

        if (relevantAssignments.Count > 0)
        {
            _context.ProjectAssignments.RemoveRange(relevantAssignments);
            foreach (var assignment in relevantAssignments)
            {
                request.Project.Assignments.Remove(assignment);
            }
        }

        var now = DateTime.UtcNow;

        foreach (var developerId in developerIds)
        {
            var assignment = new ProjectAssignment
            {
                ProjectId = request.Project.Id,
                UserId = developerId,
                AssignedRole = model.LeadUserId == developerId ? RoleConstants.Roles.EkipLideri : RoleConstants.Roles.Yazilimci,
                AssignedOn = now,
                CompletionPercent = 0,
                Project = request.Project
            };

            request.Project.Assignments.Add(assignment);
            await _context.ProjectAssignments.AddAsync(assignment, cancellationToken);
        }

        foreach (var testerId in testerIds)
        {
            var assignment = new ProjectAssignment
            {
                ProjectId = request.Project.Id,
                UserId = testerId,
                AssignedRole = RoleConstants.Roles.TestYazilimcisi,
                AssignedOn = now,
                CompletionPercent = 0,
                Project = request.Project
            };

            request.Project.Assignments.Add(assignment);
            await _context.ProjectAssignments.AddAsync(assignment, cancellationToken);
        }

        request.Project.LeadUserId = model.LeadUserId;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var assignedUsers = selectedUsers;

        var notifiedUsers = assignedUsers
            .Where(u => u.Id != actingUserId)
            .ToList();

        var developerNames = assignedUsers
            .Where(u => developerIds.Contains(u.Id))
            .Select(u => u.FullName ?? u.UserName ?? u.Id.ToString())
            .ToArray();

        await _auditLogService.RecordAsync(actingUserId, "Proje Görevlendirme", nameof(Project), request.Project.Id,
            $"Lider: {(model.LeadUserId.HasValue ? model.LeadUserId.Value.ToString() : "-")}, Geliştiriciler: {string.Join(", ", developerNames)}", cancellationToken);

        if (notifiedUsers.Count > 0)
        {
            await _notificationService.SendAsync(notifiedUsers,
                "Yeni Proje Görevlendirmesi",
                $"\"{request.Title}\" talebi için görev atandınız.",
                $"/Requests/Detay/{request.Id}",
                cancellationToken);
        }
    }

    public async Task<AssessmentHistoryViewModel> GetAssessmentHistoryAsync(
        int assessorUserId,
        DateTime? baslangic,
        DateTime? bitis,
        AssessmentResult? karar,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RequestAssessments
            .Include(a => a.Request)
            .Where(a => a.AssessedByUserId == assessorUserId && a.AssessedOn != null && a.Result != AssessmentResult.Beklemede);

        if (baslangic.HasValue)
        {
            var start = baslangic.Value.Date;
            query = query.Where(a => a.AssessedOn >= start);
        }

        if (bitis.HasValue)
        {
            var end = bitis.Value.Date.AddDays(1);
            query = query.Where(a => a.AssessedOn < end);
        }

        if (karar.HasValue)
        {
            query = query.Where(a => a.Result == karar.Value);
        }

        var assessments = await query
            .OrderByDescending(a => a.AssessedOn)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = assessments
            .Select(a => new AssessmentHistoryItemViewModel
            {
                AssessmentId = a.Id,
                RequestId = a.RequestId,
                TalepBasligi = a.Request?.Title ?? $"Talep #{a.RequestId}",
                Tarih = a.AssessedOn,
                Sonuc = a.Result,
                KisaGerekce = string.IsNullOrWhiteSpace(a.Notes) ? null : a.Notes.Length > 120 ? a.Notes[..120] + "…" : a.Notes
            })
            .ToArray();

        return new AssessmentHistoryViewModel
        {
            Baslangic = baslangic,
            Bitis = bitis,
            Karar = karar,
            Kayitlar = items
        };
    }

    private static IReadOnlyCollection<WorkflowDefinitionViewModel> BuildWorkflowViewModels(Project? project)
    {
        if (project?.WorkflowDefinitions is null || project.WorkflowDefinitions.Count == 0)
        {
            return Array.Empty<WorkflowDefinitionViewModel>();
        }

        return project.WorkflowDefinitions
            .OrderBy(w => w.DisplayOrder)
            .ThenBy(w => w.Id)
            .Select(w => new WorkflowDefinitionViewModel
            {
                Id = w.Id,
                Title = w.Title,
                Steps = w.Steps
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Id)
                    .Select(s => new WorkflowStepViewModel
                    {
                        Id = s.Id,
                        SequenceCode = s.SequenceCode,
                        Description = s.Description,
                        StepType = s.StepType,
                        Role = s.Role,
                        NextStepCode = s.NextStepCode,
                        NextStepYesCode = s.NextStepYesCode,
                        NextStepNoCode = s.NextStepNoCode
                    })
                    .ToArray()
            })
            .ToArray();
    }

    private static List<WorkflowEditorDefinition> NormalizeWorkflowPayload(WorkflowEditorPostModel model)
    {
        var definitions = model?.Workflows ?? new List<WorkflowEditorDefinition>();
        var result = new List<WorkflowEditorDefinition>();
        var definitionIndex = 0;

        foreach (var definition in definitions)
        {
            if (definition is null)
            {
                continue;
            }

            var title = definition.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException("İş akışı başlığı boş bırakılamaz.");
            }

            var prefix = GetSequencePrefix(definitionIndex);
            var steps = NormalizeWorkflowSteps(definition.Steps ?? new List<WorkflowEditorStep>(), prefix);
            if (steps.Count == 0)
            {
                throw new InvalidOperationException($"\"{title}\" iş akışı için en az bir adım eklenmelidir.");
            }

            result.Add(new WorkflowEditorDefinition
            {
                Id = definition.Id,
                Title = TrimToLength(title, 150),
                Steps = steps
            });

            definitionIndex++;
        }

        return result;
    }

    private static List<WorkflowEditorStep> NormalizeWorkflowSteps(IEnumerable<WorkflowEditorStep> steps, string sequencePrefix)
    {
        var result = new List<WorkflowEditorStep>();
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nextIndex = 1;
        var prefix = string.IsNullOrWhiteSpace(sequencePrefix)
            ? "A"
            : sequencePrefix.ToUpperInvariant();
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in steps ?? Enumerable.Empty<WorkflowEditorStep>())
        {
            if (step is null)
            {
                continue;
            }

            var description = step.Description?.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new InvalidOperationException("Adım açıklaması boş bırakılamaz.");
            }

            var rawCode = step.SequenceCode?.Trim();
            var normalizedOriginalCode = string.IsNullOrWhiteSpace(rawCode)
                ? null
                : TrimToLength(rawCode.ToUpperInvariant(), 20);
            var code = normalizedOriginalCode;

            if (string.IsNullOrWhiteSpace(code) || !code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                code = GenerateSequenceCode(usedCodes, ref nextIndex, prefix);
            }
            else if (!usedCodes.Add(code))
            {
                code = GenerateSequenceCode(usedCodes, ref nextIndex, prefix);
            }

            code = TrimToLength(code, 20);

            if (!string.IsNullOrWhiteSpace(normalizedOriginalCode) && !normalizedOriginalCode.Equals(code, StringComparison.OrdinalIgnoreCase))
            {
                replacements[normalizedOriginalCode] = code;
            }

            UpdateNextIndexFromCode(code, prefix, ref nextIndex);

            var stepType = Enum.IsDefined(typeof(WorkflowStepType), step.StepType)
                ? step.StepType
                : WorkflowStepType.Normal;

            var role = step.Role?.Trim();
            var next = step.NextStepCode?.Trim();
            var nextYes = step.NextStepYesCode?.Trim();
            var nextNo = step.NextStepNoCode?.Trim();

            if (stepType == WorkflowStepType.KararNoktasi)
            {
                if (!string.IsNullOrWhiteSpace(nextYes))
                {
                    nextYes = nextYes.ToUpperInvariant();
                }

                if (!string.IsNullOrWhiteSpace(nextNo))
                {
                    nextNo = nextNo.ToUpperInvariant();
                }

                result.Add(new WorkflowEditorStep
                {
                    Id = step.Id,
                    SequenceCode = TrimToLength(code, 20),
                    Description = TrimToLength(description, 500),
                    StepType = WorkflowStepType.KararNoktasi,
                    Role = TrimToLengthOrNull(role, 150),
                    NextStepCode = null,
                    NextStepYesCode = TrimToLengthOrNull(nextYes, 50),
                    NextStepNoCode = TrimToLengthOrNull(nextNo, 50)
                });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(next))
                {
                    next = next.ToUpperInvariant();
                }

                result.Add(new WorkflowEditorStep
                {
                    Id = step.Id,
                    SequenceCode = TrimToLength(code, 20),
                    Description = TrimToLength(description, 500),
                    StepType = WorkflowStepType.Normal,
                    Role = TrimToLengthOrNull(role, 150),
                    NextStepCode = TrimToLengthOrNull(next, 50),
                    NextStepYesCode = null,
                    NextStepNoCode = null
                });
            }
        }

        if (replacements.Count > 0)
        {
            foreach (var step in result)
            {
                if (step.StepType == WorkflowStepType.KararNoktasi)
                {
                    step.NextStepYesCode = ReplaceSequenceReference(step.NextStepYesCode, replacements);
                    step.NextStepNoCode = ReplaceSequenceReference(step.NextStepNoCode, replacements);
                }
                else
                {
                    step.NextStepCode = ReplaceSequenceReference(step.NextStepCode, replacements);
                }
            }
        }

        return result;
    }

    private void ApplyWorkflowUpdates(Project project, IReadOnlyList<WorkflowEditorDefinition> payload)
    {
        var existingDefinitions = project.WorkflowDefinitions
            .ToDictionary(w => w.Id);

        var keepIds = payload
            .Where(w => w.Id.HasValue && w.Id.Value > 0)
            .Select(w => w.Id!.Value)
            .ToHashSet();

        var definitionsToRemove = project.WorkflowDefinitions
            .Where(w => !keepIds.Contains(w.Id))
            .ToList();

        if (definitionsToRemove.Count > 0)
        {
            foreach (var definition in definitionsToRemove)
            {
                project.WorkflowDefinitions.Remove(definition);
                _context.WorkflowDefinitions.Remove(definition);
            }
        }

        for (var index = 0; index < payload.Count; index++)
        {
            var item = payload[index];

            if (item.Id.HasValue && item.Id.Value > 0 && existingDefinitions.TryGetValue(item.Id.Value, out var definition))
            {
                definition.Title = item.Title;
                definition.DisplayOrder = index;
                ApplyStepUpdates(definition, item.Steps);
            }
            else
            {
                var newDefinition = new WorkflowDefinition
                {
                    Title = item.Title,
                    ProjectId = project.Id,
                    DisplayOrder = index,
                    CreatedAt = DateTime.UtcNow
                };

                for (var stepIndex = 0; stepIndex < item.Steps.Count; stepIndex++)
                {
                    var step = item.Steps[stepIndex];
                    newDefinition.Steps.Add(new WorkflowStep
                    {
                        SequenceCode = step.SequenceCode,
                        Description = step.Description,
                        StepType = step.StepType,
                        Role = step.Role,
                        NextStepCode = step.StepType == WorkflowStepType.Normal ? step.NextStepCode : null,
                        NextStepYesCode = step.StepType == WorkflowStepType.KararNoktasi ? step.NextStepYesCode : null,
                        NextStepNoCode = step.StepType == WorkflowStepType.KararNoktasi ? step.NextStepNoCode : null,
                        DisplayOrder = stepIndex
                    });
                }

                project.WorkflowDefinitions.Add(newDefinition);
            }
        }
    }

    private void ApplyStepUpdates(WorkflowDefinition definition, IReadOnlyList<WorkflowEditorStep> steps)
    {
        var existingSteps = definition.Steps.ToDictionary(s => s.Id);
        var keepIds = steps
            .Where(s => s.Id.HasValue && s.Id.Value > 0)
            .Select(s => s.Id!.Value)
            .ToHashSet();

        var stepsToRemove = definition.Steps
            .Where(s => !keepIds.Contains(s.Id))
            .ToList();

        foreach (var step in stepsToRemove)
        {
            definition.Steps.Remove(step);
            _context.WorkflowSteps.Remove(step);
        }

        for (var index = 0; index < steps.Count; index++)
        {
            var model = steps[index];

            if (model.Id.HasValue && model.Id.Value > 0 && existingSteps.TryGetValue(model.Id.Value, out var entity))
            {
                entity.SequenceCode = model.SequenceCode;
                entity.Description = model.Description;
                entity.StepType = model.StepType;
                entity.Role = model.Role;
                if (model.StepType == WorkflowStepType.KararNoktasi)
                {
                    entity.NextStepCode = null;
                    entity.NextStepYesCode = model.NextStepYesCode;
                    entity.NextStepNoCode = model.NextStepNoCode;
                }
                else
                {
                    entity.NextStepCode = model.NextStepCode;
                    entity.NextStepYesCode = null;
                    entity.NextStepNoCode = null;
                }
                entity.DisplayOrder = index;
            }
            else
            {
                definition.Steps.Add(new WorkflowStep
                {
                    SequenceCode = model.SequenceCode,
                    Description = model.Description,
                    StepType = model.StepType,
                    Role = model.Role,
                    NextStepCode = model.StepType == WorkflowStepType.Normal ? model.NextStepCode : null,
                    NextStepYesCode = model.StepType == WorkflowStepType.KararNoktasi ? model.NextStepYesCode : null,
                    NextStepNoCode = model.StepType == WorkflowStepType.KararNoktasi ? model.NextStepNoCode : null,
                    DisplayOrder = index
                });
            }
        }
    }

    private static string GenerateSequenceCode(HashSet<string> usedCodes, ref int nextIndex, string prefix)
    {
        if (nextIndex < 1)
        {
            nextIndex = 1;
        }

        var normalizedPrefix = string.IsNullOrWhiteSpace(prefix)
            ? "A"
            : prefix.ToUpperInvariant();

        while (true)
        {
            var candidate = $"{normalizedPrefix}{nextIndex}";
            nextIndex++;
            if (usedCodes.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private static void UpdateNextIndexFromCode(string code, string prefix, ref int nextIndex)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(prefix))
        {
            return;
        }

        if (!code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var suffix = code[prefix.Length..];
        if (int.TryParse(suffix, out var number) && number >= nextIndex)
        {
            nextIndex = number + 1;
        }
    }

    private static string GetSequencePrefix(int index)
    {
        if (index < 0)
        {
            index = 0;
        }

        var value = index;
        Span<char> buffer = stackalloc char[8];
        var position = buffer.Length;

        value++;

        while (value > 0)
        {
            value--;
            var remainder = value % 26;
            buffer[--position] = (char)('A' + remainder);
            value /= 26;
        }

        return new string(buffer[position..]);
    }

    private static string? ReplaceSequenceReference(string? value, IDictionary<string, string> replacements)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var key = value.ToUpperInvariant();
        return replacements.TryGetValue(key, out var replacement)
            ? replacement
            : key;
    }

    private static string TrimToLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? TrimToLengthOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static bool IsDeveloper(UserProfile user) => user.Role == UserRole.Yazilimci;

    private bool IsDiscussionParticipant(UserProfile user, SoftwareRequest request)
    {
        if (user.Role == UserRole.Admin)
        {
            return true;
        }

        if (IsDeveloper(user))
        {
            return request.Project?.Assignments.Any(a => a.UserId == user.Id) == true;
        }

        if (user.Role == UserRole.BirimKullanicisi || user.Role == UserRole.BirimYetkilisi)
        {
            return user.DepartmentId == request.DepartmentId;
        }

        return false;
    }

    private async Task<UserProfile> GetUserAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Kullanıcı bulunamadı");
    }

    private async Task<List<UserProfile>> GetUsersByRolesAsync(CancellationToken cancellationToken, params UserRole[] roles)
    {
        return await _context.UserProfiles
            .Where(u => u.IsActive && roles.Contains(u.Role))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<UserProfile>> GetDepartmentRoleUsersAsync(int departmentId, CancellationToken cancellationToken, params UserRole[] roles)
    {
        return await _context.UserProfiles
            .Where(u => u.IsActive && u.DepartmentId == departmentId && roles.Contains(u.Role))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<UserProfile>> GetProjectDevelopersAsync(int projectId, CancellationToken cancellationToken)
    {
        var developerRoles = new[]
        {
            RoleConstants.Roles.Yazilimci,
            RoleConstants.Roles.EkipLideri,
            RoleConstants.Roles.TestYazilimcisi
        };

        var developers = await _context.ProjectAssignments
            .Where(a => a.ProjectId == projectId && developerRoles.Contains(a.AssignedRole))
            .Include(a => a.User)
            .Select(a => a.User)
            .Where(u => u != null && u.IsActive)
            .ToListAsync(cancellationToken);

        return developers
            .Where(u => u is not null)
            .GroupBy(u => u!.Id)
            .Select(g => g.First()!)
            .ToList();
    }

    private static AssessmentStage MapStage(UserRole role) => role switch
    {
        UserRole.DegerlendiriciBir => AssessmentStage.DegerlendiriciBir,
        UserRole.DegerlendiriciIki => AssessmentStage.DegerlendiriciIki,
        UserRole.DegerlendiriciUc => AssessmentStage.DegerlendiriciUc,
        UserRole.DegerlendirmeBaskani => AssessmentStage.BaskanOnayi,
        _ => throw new InvalidOperationException("Değerlendirme yetkiniz bulunmuyor.")
    };

    private RequestOverviewViewModel CreateOverview(string title, IReadOnlyCollection<SoftwareRequest> requests)
    {
        var items = requests
            .Select(r => new RequestListItemViewModel
            {
                Id = r.Id,
                Baslik = r.Title,
                Durum = GetStatusName(r.Status),
                BirimAdi = r.Department?.Name ?? string.Empty,
                TalepSahibi = r.RequestedByUser?.FullName ?? r.RequestedByUser?.UserName ?? string.Empty,
                Oncelik = r.Priority,
                OlusturmaTarihi = r.CreatedAt
            })
            .ToArray();

        return new RequestOverviewViewModel
        {
            Baslik = title,
            Talepler = items
        };
    }

    private static string GetStatusName(RequestStatus status) => status switch
    {
        RequestStatus.OnayBekleniyor => "Onay Bekleniyor",
        RequestStatus.Onaylandi => "Onaylandı",
        RequestStatus.Reddedildi => "Reddedildi",
        RequestStatus.Degerlendirmede => "Değerlendirmede",
        RequestStatus.BaskanOnayiBekliyor => "Başkan Onayı Bekliyor",
        RequestStatus.Gelistirmede => "Geliştirme Sürecinde",
        RequestStatus.Tamamlandi => "Tamamlandı",
        RequestStatus.Kapandi => "Kapandı",
        _ => "Taslak"
    };

    private async Task EnsureProjectAsync(SoftwareRequest request, CancellationToken cancellationToken)
    {
        if (request.Project is not null)
        {
            await _context.Entry(request.Project).Collection(p => p.Assignments).LoadAsync(cancellationToken);
            return;
        }

        var project = new Project
        {
            RequestId = request.Id,
            Name = request.Title,
            Description = request.Description,
            Status = ProjectStatus.Analiz,
            StartDate = DateTime.UtcNow
        };

        await _context.Projects.AddAsync(project, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        request.Project = project;
    }

    private async Task EnsureReleasedSoftwareAsync(SoftwareRequest request, CancellationToken cancellationToken)
    {
        if (request.Project is null)
        {
            return;
        }

        if (request.Project.ReleasedSoftwareId.HasValue)
        {
            request.ExistingSoftwareId ??= request.Project.ReleasedSoftwareId;
            return;
        }

        var software = new Software
        {
            Name = string.IsNullOrWhiteSpace(request.Project.Name) ? request.Title : request.Project.Name,
            Description = string.IsNullOrWhiteSpace(request.Project.Description) ? request.Description : request.Project.Description,
            DepartmentId = request.DepartmentId,
            Category = request.Department?.Name,
            SupportContact = request.Department?.ContactEmail
        };

        _context.Softwares.Add(software);
        await _context.SaveChangesAsync(cancellationToken);

        request.Project.ReleasedSoftwareId = software.Id;
        request.Project.ReleasedSoftware = software;
        request.ExistingSoftwareId = software.Id;

        var assignments = request.Project.Assignments;
        if (assignments.Count == 0)
        {
            await _context.Entry(request.Project).Collection(p => p.Assignments).LoadAsync(cancellationToken);
            assignments = request.Project.Assignments;
        }

        var uniqueAssignments = assignments
            .Select(a => a.UserId)
            .Distinct()
            .ToList();

        var addedPairs = new HashSet<(int UserId, SoftwareResponsibilityType Responsibility)>();
        var responsibilities = new List<SoftwareResponsibility>();

        void AddResponsibility(int userId, SoftwareResponsibilityType type)
        {
            if (userId <= 0)
            {
                return;
            }

            if (addedPairs.Add((userId, type)))
            {
                responsibilities.Add(new SoftwareResponsibility
                {
                    SoftwareId = software.Id,
                    UserId = userId,
                    ResponsibilityType = type
                });
            }
        }

        foreach (var developerId in uniqueAssignments)
        {
            AddResponsibility(developerId, SoftwareResponsibilityType.Yazilimci);
        }

        if (request.RequestedByUser is not null)
        {
            if (request.RequestedByUser.Role == UserRole.BirimKullanicisi)
            {
                AddResponsibility(request.RequestedByUser.Id, SoftwareResponsibilityType.BirimKullanicisi);
            }
            else if (request.RequestedByUser.Role == UserRole.BirimYetkilisi)
            {
                AddResponsibility(request.RequestedByUser.Id, SoftwareResponsibilityType.BirimYetkilisi);
            }
        }

        if (request.DepartmentId > 0)
        {
            var departmentManagers = await _context.UserProfiles
                .Where(u => u.IsActive
                            && u.DepartmentId == request.DepartmentId
                            && u.Role == UserRole.BirimYetkilisi)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            foreach (var managerId in departmentManagers)
            {
                AddResponsibility(managerId, SoftwareResponsibilityType.BirimYetkilisi);
            }
        }

        if (responsibilities.Count > 0)
        {
            await _context.SoftwareResponsibilities.AddRangeAsync(responsibilities, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
