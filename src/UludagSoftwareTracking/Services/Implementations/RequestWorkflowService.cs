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

namespace UludagSoftwareTracking.Services.Implementations;

public class RequestWorkflowService : IRequestWorkflowService
{
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

    public async Task<RequestOverviewViewModel> GetPendingAssessmentsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _context.SoftwareRequests
            .Where(r => r.Status == RequestStatus.Degerlendirmede)
            .Include(r => r.Department)
            .Include(r => r.RequestedByUser)
            .OrderBy(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return CreateOverview("Değerlendirme Bekleyen Talepler", requests);
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
                .OrderByDescending(a => a.AssessedOn ?? request.CreatedAt)
                .ToArray(),
            Proje = request.Project
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

    public async Task AssessAsync(RequestAssessmentInputModel model, int assessorUserId, CancellationToken cancellationToken = default)
    {
        var request = await _context.SoftwareRequests
            .Include(r => r.Assessments)
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == model.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadı");

        var assessment = new RequestAssessment
        {
            RequestId = request.Id,
            AssessedByUserId = assessorUserId,
            Result = model.Result,
            Notes = model.Notes,
            ExistingSoftwareId = model.ExistingSoftwareId,
            AssessedOn = DateTime.UtcNow
        };

        request.Assessments.Add(assessment);
        request.UpdatedAt = DateTime.UtcNow;

        switch (model.Result)
        {
            case AssessmentResult.Uygun:
                request.Status = RequestStatus.Gelistirmede;
                break;
            case AssessmentResult.YeniGelistirme:
                request.Status = RequestStatus.Gelistirmede;
                await EnsureProjectAsync(request, cancellationToken);
                break;
            case AssessmentResult.VarOlanYazilimaYonlendirildi:
                request.Status = RequestStatus.Kapandi;
                break;
            case AssessmentResult.UygunDegil:
                request.Status = RequestStatus.Reddedildi;
                request.RejectionReason = model.Notes;
                break;
            default:
                request.Status = RequestStatus.Degerlendirmede;
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private RequestOverviewViewModel CreateOverview(string title, IReadOnlyCollection<SoftwareRequest> requests)
    {
        var items = requests
            .Select(r => new RequestListItemViewModel
            {
                Id = r.Id,
                Baslik = r.Title,
                Durum = GetStatusName(r.Status),
                BirimAdi = r.Department?.Name ?? string.Empty,
                TalepSahibi = r.RequestedByUser?.FullName ?? r.RequestedByUser?.UserName ?? "",
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
        RequestStatus.Gelistirmede => "Geliştirme Sürecinde",
        RequestStatus.Tamamlandi => "Tamamlandı",
        RequestStatus.Kapandi => "Kapandı",
        _ => "Taslak"
    };

    private async Task EnsureProjectAsync(SoftwareRequest request, CancellationToken cancellationToken)
    {
        if (request.Project is not null)
        {
            return;
        }

        var lead = await _context.UserProfiles
            .Where(u => u.Role == UserRole.YazilimEkibiLideri)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var project = new Project
        {
            RequestId = request.Id,
            Name = request.Title,
            Description = request.Description,
            Status = ProjectStatus.Planlama,
            StartDate = DateTime.UtcNow,
            LeadUserId = lead?.Id
        };

        await _context.Projects.AddAsync(project, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
