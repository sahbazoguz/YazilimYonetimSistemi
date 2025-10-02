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

    public async Task CreateSoftwareAsync(SoftwareEditInputModel model, CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var entity = new Software
        {
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            DepartmentId = model.DepartmentId,
            SupportContact = string.IsNullOrWhiteSpace(model.SupportContact) ? null : model.SupportContact.Trim(),
            WebsiteUrl = string.IsNullOrWhiteSpace(model.WebsiteUrl) ? null : model.WebsiteUrl.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _context.Softwares.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
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

        software.Name = model.Name.Trim();
        software.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        software.DepartmentId = model.DepartmentId;
        software.SupportContact = string.IsNullOrWhiteSpace(model.SupportContact) ? null : model.SupportContact.Trim();
        software.WebsiteUrl = string.IsNullOrWhiteSpace(model.WebsiteUrl) ? null : model.WebsiteUrl.Trim();
        software.UpdatedDate = DateTime.UtcNow;

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

        software.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
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
