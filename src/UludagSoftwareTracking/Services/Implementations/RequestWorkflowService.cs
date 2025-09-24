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

    public RequestWorkflowService(ApplicationDbContext context)
    {
        _context = context;
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
                .ToArray()
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

        return request.Id;
    }

    public async Task ApproveAsync(int requestId, int approverUserId, string? notes, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Approvals)
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
    }

    public async Task RejectAsync(int requestId, int approverUserId, string? notes, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Approvals)
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
    }

    public async Task UpdateAlgorithmAsync(int requestId, int userId, string algorithmNotes, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Project is null)
        {
            throw new InvalidOperationException("Proje oluşturulmadan algoritma planı güncellenemez.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (!IsDeveloper(user) && user.Role != UserRole.Admin)
        {
            throw new InvalidOperationException("Algoritma üzerinde değişiklik yapma yetkiniz yok.");
        }

        if (IsDeveloper(user) && !request.Project.Assignments.Any(a => a.UserId == userId))
        {
            throw new InvalidOperationException("Bu projeye atanmadınız.");
        }

        var trimmed = string.IsNullOrWhiteSpace(algorithmNotes) ? null : algorithmNotes.Trim();
        request.Project.AlgorithmNotes = trimmed;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateGuidesAsync(int requestId, int userId, string? technicalGuidePath, string? userGuidePath, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
            .ThenInclude(p => p!.Assignments)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        if (request.Project is null)
        {
            throw new InvalidOperationException("Proje oluşturulmadan kılavuz bilgileri güncellenemez.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (!IsDeveloper(user) && user.Role != UserRole.Admin)
        {
            throw new InvalidOperationException("Kılavuz bilgilerini güncelleme yetkiniz yok.");
        }

        if (IsDeveloper(user) && !request.Project.Assignments.Any(a => a.UserId == userId))
        {
            throw new InvalidOperationException("Bu projeye atanmadınız.");
        }

        var hasUpdate = false;

        if (technicalGuidePath is not null)
        {
            request.Project.TechnicalGuidePath = string.IsNullOrWhiteSpace(technicalGuidePath)
                ? null
                : technicalGuidePath.Trim();
            hasUpdate = true;
        }

        if (userGuidePath is not null)
        {
            request.Project.UserGuidePath = string.IsNullOrWhiteSpace(userGuidePath)
                ? null
                : userGuidePath.Trim();
            hasUpdate = true;
        }

        if (!hasUpdate)
        {
            return;
        }

        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
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
    }

    public async Task ConfirmTestingAsync(int requestId, int userId, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
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
        }

        await _context.SaveChangesAsync(cancellationToken);
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

        var developer = await _context.UserProfiles
            .Where(u => u.Role == UserRole.Yazilimci)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (developer is not null)
        {
            project.LeadUserId = developer.Id;
            project.LeadUser = developer;

            var assignment = new ProjectAssignment
            {
                ProjectId = project.Id,
                UserId = developer.Id,
                AssignedRole = RoleConstants.Roles.Yazilimci,
                CompletionPercent = 0,
                Project = project
            };

            await _context.ProjectAssignments.AddAsync(assignment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            project.Assignments.Add(assignment);
        }

        request.Project = project;
    }
}
