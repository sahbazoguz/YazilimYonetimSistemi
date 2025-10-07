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

public class SoftwareManagementService : ISoftwareManagementService
{
    private readonly ApplicationDbContext _context;

    public SoftwareManagementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SoftwareManagementViewModel> GetManagementViewModelAsync(CancellationToken cancellationToken = default)
    {
        var softwares = await _context.Softwares
            .Include(s => s.Department)
            .Include(s => s.Responsibilities)
                .ThenInclude(r => r.User)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var developers = await LoadUsersAsync(UserRole.Yazilimci, cancellationToken);
        var unitUsers = await LoadUsersAsync(UserRole.BirimKullanicisi, cancellationToken);
        var unitManagers = await LoadUsersAsync(UserRole.BirimYetkilisi, cancellationToken);

        var items = softwares
            .Select(s => new SoftwareManagementItemViewModel
            {
                Id = s.Id,
                Ad = s.Name,
                Aciklama = s.Description,
                BirimAdi = s.Department?.Name,
                DestekEposta = s.SupportContact,
                WebAdresi = s.WebsiteUrl,
                Yazilimcilar = s.Responsibilities
                    .Where(r => r.ResponsibilityType == SoftwareResponsibilityType.Yazilimci && r.User != null)
                    .Select(r => r.User!.FullName ?? r.User.UserName ?? r.User.Id.ToString())
                    .OrderBy(ad => ad)
                    .ToArray(),
                BirimKullanicilari = s.Responsibilities
                    .Where(r => r.ResponsibilityType == SoftwareResponsibilityType.BirimKullanicisi && r.User != null)
                    .Select(r => r.User!.FullName ?? r.User.UserName ?? r.User.Id.ToString())
                    .OrderBy(ad => ad)
                    .ToArray(),
                BirimYetkilileri = s.Responsibilities
                    .Where(r => r.ResponsibilityType == SoftwareResponsibilityType.BirimYetkilisi && r.User != null)
                    .Select(r => r.User!.FullName ?? r.User.UserName ?? r.User.Id.ToString())
                    .OrderBy(ad => ad)
                    .ToArray(),
                Duzenleme = new SoftwareEditInputModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    DepartmentId = s.DepartmentId,
                    SupportContact = s.SupportContact,
                    WebsiteUrl = s.WebsiteUrl
                },
                Sorumluluklar = new SoftwareResponsibilityInputModel
                {
                    SoftwareId = s.Id,
                    YazilimciIdleri = s.Responsibilities
                        .Where(r => r.ResponsibilityType == SoftwareResponsibilityType.Yazilimci)
                        .Select(r => r.UserId)
                        .Distinct()
                        .ToList(),
                    BirimKullanicisiIdleri = s.Responsibilities
                        .Where(r => r.ResponsibilityType == SoftwareResponsibilityType.BirimKullanicisi)
                        .Select(r => r.UserId)
                        .Distinct()
                        .ToList(),
                    BirimYetkilisiIdleri = s.Responsibilities
                        .Where(r => r.ResponsibilityType == SoftwareResponsibilityType.BirimYetkilisi)
                        .Select(r => r.UserId)
                        .Distinct()
                        .ToList()
                }
            })
            .ToArray();

        return new SoftwareManagementViewModel
        {
            Yazilimlar = items,
            Birimler = departments,
            YazilimciAdaylari = developers,
            BirimKullanicisiAdaylari = unitUsers,
            BirimYetkilisiAdaylari = unitManagers,
            YeniYazilim = new SoftwareEditInputModel()
        };
    }

    public async Task CreateSoftwareAsync(SoftwareEditInputModel model, int actingUserId, CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (model.DepartmentId is null)
        {
            throw new InvalidOperationException("Manuel katalog kaydı için birim seçilmelidir.");
        }

        var now = DateTime.UtcNow;
        var trimmedName = model.Name.Trim();
        var sanitizedDescription = string.IsNullOrWhiteSpace(model.Description)
            ? $"{trimmedName} manuel katalog kaydı."
            : model.Description.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var departmentId = model.DepartmentId.Value;
        if (departmentId <= 0)
        {
            throw new InvalidOperationException("Manuel katalog kaydı için geçerli birim seçilmelidir.");
        }
        var software = new Software
        {
            Name = trimmedName,
            Description = sanitizedDescription,
            DepartmentId = departmentId,
            SupportContact = string.IsNullOrWhiteSpace(model.SupportContact) ? null : model.SupportContact.Trim(),
            WebsiteUrl = string.IsNullOrWhiteSpace(model.WebsiteUrl) ? null : model.WebsiteUrl.Trim(),
            IsActive = true,
            CreatedDate = now
        };

        await _context.Softwares.AddAsync(software, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var request = new SoftwareRequest
        {
            Title = trimmedName,
            Description = sanitizedDescription,
            DepartmentId = departmentId,
            RequestedByUserId = actingUserId,
            Priority = RequestPriority.Orta,
            Status = RequestStatus.Gelistirmede,
            CreatedAt = now,
            UpdatedAt = now,
            ExistingSoftwareId = software.Id
        };

        await _context.SoftwareRequests.AddAsync(request, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var project = new Project
        {
            RequestId = request.Id,
            Name = trimmedName,
            Description = sanitizedDescription,
            Status = ProjectStatus.Gelistirme,
            StartDate = now,
            ReleasedSoftwareId = software.Id
        };

        await _context.Projects.AddAsync(project, cancellationToken);

        request.Project = project;

        var approval = new RequestApproval
        {
            RequestId = request.Id,
            Status = ApprovalStatus.Onaylandi,
            DecidedAt = now,
            Notes = "Manuel katalog kaydı",
            ApprovedByUserId = actingUserId
        };

        var assessment = new RequestAssessment
        {
            RequestId = request.Id,
            Stage = AssessmentStage.BaskanOnayi,
            Result = AssessmentResult.YeniGelistirme,
            AssessedOn = now,
            Notes = "Manuel katalog kaydı",
            AssessedByUserId = actingUserId,
            ExistingSoftwareId = software.Id
        };

        await _context.RequestApprovals.AddAsync(approval, cancellationToken);
        await _context.RequestAssessments.AddAsync(assessment, cancellationToken);

        software.CatalogRequestId = request.Id;
        software.CatalogRequest = request;
        software.UpdatedDate = now;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateSoftwareAsync(SoftwareEditInputModel model, CancellationToken cancellationToken = default)
    {
        if (model?.Id is null)
        {
            throw new ArgumentException("Güncellenecek yazılım bulunamadı.");
        }

        var software = await _context.Softwares.FirstOrDefaultAsync(s => s.Id == model.Id.Value, cancellationToken);
        if (software is null)
        {
            throw new InvalidOperationException("Yazılım kaydı bulunamadı.");
        }

        var sanitizedDescription = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

        if (model.DepartmentId is null || model.DepartmentId.Value <= 0)
        {
            throw new InvalidOperationException("Geçerli birim seçilmelidir.");
        }

        software.Name = model.Name.Trim();
        software.Description = sanitizedDescription;
        software.DepartmentId = model.DepartmentId.Value;
        software.SupportContact = string.IsNullOrWhiteSpace(model.SupportContact) ? null : model.SupportContact.Trim();
        software.WebsiteUrl = string.IsNullOrWhiteSpace(model.WebsiteUrl) ? null : model.WebsiteUrl.Trim();
        software.UpdatedDate = DateTime.UtcNow;

        if (software.CatalogRequestId.HasValue)
        {
            var request = await _context.SoftwareRequests
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == software.CatalogRequestId.Value, cancellationToken);

            if (request is not null)
            {
                software.CatalogRequest = request;
                var fallbackDescription = sanitizedDescription ?? software.Name;
                request.Title = software.Name;
                request.Description = fallbackDescription;
                request.UpdatedAt = DateTime.UtcNow;

                if (software.DepartmentId.HasValue)
                {
                    request.DepartmentId = software.DepartmentId.Value;
                }

                if (request.Project is not null)
                {
                    request.Project.Name = software.Name;
                    request.Project.Description = fallbackDescription;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSoftwareAsync(int softwareId, CancellationToken cancellationToken = default)
    {
        var software = await _context.Softwares.FirstOrDefaultAsync(s => s.Id == softwareId, cancellationToken);
        if (software is null)
        {
            return;
        }

        _context.Softwares.Remove(software);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateResponsibilitiesAsync(SoftwareResponsibilityInputModel model, CancellationToken cancellationToken = default)
    {
        var software = await _context.Softwares
            .Include(s => s.Responsibilities)
            .FirstOrDefaultAsync(s => s.Id == model.SoftwareId, cancellationToken)
            ?? throw new InvalidOperationException("Yazılım kaydı bulunamadı.");

        var developerIds = await FilterUsersByRoleAsync(model.YazilimciIdleri, UserRole.Yazilimci, cancellationToken);
        var unitUserIds = await FilterUsersByRoleAsync(model.BirimKullanicisiIdleri, UserRole.BirimKullanicisi, cancellationToken);
        var unitManagerIds = await FilterUsersByRoleAsync(model.BirimYetkilisiIdleri, UserRole.BirimYetkilisi, cancellationToken);

        SyncResponsibilities(software, SoftwareResponsibilityType.Yazilimci, developerIds);
        SyncResponsibilities(software, SoftwareResponsibilityType.BirimKullanicisi, unitUserIds);
        SyncResponsibilities(software, SoftwareResponsibilityType.BirimYetkilisi, unitManagerIds);

        await SyncCatalogAssignmentsAsync(software, developerIds, cancellationToken);

        software.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncCatalogAssignmentsAsync(Software software, IReadOnlyCollection<int> developerIds, CancellationToken cancellationToken)
    {
        if (!software.CatalogRequestId.HasValue)
        {
            return;
        }

        var request = await _context.SoftwareRequests
            .Include(r => r.Project)
                .ThenInclude(p => p!.Assignments)
            .FirstOrDefaultAsync(r => r.Id == software.CatalogRequestId.Value, cancellationToken);

        if (request?.Project is null)
        {
            return;
        }

        if (request.Project.Status == ProjectStatus.Planlama || request.Project.Status == ProjectStatus.Analiz)
        {
            request.Project.Status = ProjectStatus.Gelistirme;
        }

        var developerRoles = new[]
        {
            RoleConstants.Roles.Yazilimci,
            RoleConstants.Roles.EkipLideri,
            RoleConstants.Roles.TestYazilimcisi
        };

        var existingAssignments = request.Project.Assignments
            .Where(a => developerRoles.Contains(a.AssignedRole))
            .ToList();

        if (existingAssignments.Count > 0)
        {
            _context.ProjectAssignments.RemoveRange(existingAssignments);
            foreach (var assignment in existingAssignments)
            {
                request.Project.Assignments.Remove(assignment);
            }
        }

        if (developerIds.Count == 0)
        {
            request.UpdatedAt = DateTime.UtcNow;
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var developerId in developerIds)
        {
            var assignment = new ProjectAssignment
            {
                ProjectId = request.Project.Id,
                UserId = developerId,
                AssignedRole = RoleConstants.Roles.Yazilimci,
                AssignedOn = now,
                CompletionPercent = 0
            };

            request.Project.Assignments.Add(assignment);
            await _context.ProjectAssignments.AddAsync(assignment, cancellationToken);
        }

        request.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<List<int>> FilterUsersByRoleAsync(IEnumerable<int>? userIds, UserRole expectedRole, CancellationToken cancellationToken)
    {
        if (userIds is null)
        {
            return new List<int>();
        }

        var filteredIds = userIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (filteredIds.Length == 0)
        {
            return new List<int>();
        }

        return await _context.UserProfiles
            .Where(u => u.IsActive && u.Role == expectedRole && filteredIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }

    private void SyncResponsibilities(Software software, SoftwareResponsibilityType type, IReadOnlyCollection<int> targetUserIds)
    {
        var mevcut = software.Responsibilities
            .Where(r => r.ResponsibilityType == type)
            .ToList();

        foreach (var responsibility in mevcut)
        {
            if (!targetUserIds.Contains(responsibility.UserId))
            {
                _context.SoftwareResponsibilities.Remove(responsibility);
                software.Responsibilities.Remove(responsibility);
            }
        }

        foreach (var userId in targetUserIds)
        {
            if (mevcut.All(r => r.UserId != userId))
            {
                var yeni = new SoftwareResponsibility
                {
                    SoftwareId = software.Id,
                    UserId = userId,
                    ResponsibilityType = type
                };

                software.Responsibilities.Add(yeni);
                _context.SoftwareResponsibilities.Add(yeni);
            }
        }
    }

    private async Task<IReadOnlyList<SelectableUserViewModel>> LoadUsersAsync(UserRole role, CancellationToken cancellationToken)
    {
        return await _context.UserProfiles
            .Where(u => u.IsActive && u.Role == role)
            .Include(u => u.Department)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectableUserViewModel
            {
                Id = u.Id,
                AdSoyad = string.IsNullOrWhiteSpace(u.FullName) ? u.UserName : u.FullName!,
                Birim = u.Department != null ? u.Department.Name : "Birim atanmadı"
            })
            .ToListAsync(cancellationToken);
    }
}
