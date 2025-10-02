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

public class ManualService : IManualService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;

    public ManualService(ApplicationDbContext context, IAuditLogService auditLogService, INotificationService notificationService)
    {
        _context = context;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
    }

    public async Task<ManualOverviewViewModel> GetManualsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var responsibilities = await _context.SoftwareResponsibilities
            .Where(r => r.UserId == userId)
            .Include(r => r.Software)
                .ThenInclude(s => s.Department)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (responsibilities.Count == 0)
        {
            return new ManualOverviewViewModel();
        }

        var softwareIds = responsibilities
            .Select(r => r.SoftwareId)
            .Distinct()
            .ToArray();

        var manuals = await _context.SoftwareManuals
            .Where(m => softwareIds.Contains(m.SoftwareId))
            .Include(m => m.UploadedByUser)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var manualLookup = manuals
            .GroupBy(m => m.SoftwareId)
            .ToDictionary(g => g.Key, g => g
                .OrderByDescending(m => m.UploadedOn)
                .ToList());

        var items = responsibilities
            .GroupBy(r => r.SoftwareId)
            .Select(group =>
            {
                var software = group.First().Software;
                if (software is null)
                {
                    return null;
                }

                manualLookup.TryGetValue(software.Id, out var manualList);

                SoftwareManual? teknik = manualList?
                    .FirstOrDefault(m => m.ManualType == ManualType.TeknikKilavuz);
                SoftwareManual? kullanim = manualList?
                    .FirstOrDefault(m => m.ManualType == ManualType.KullanimKilavuzu);

                var roller = group
                    .Select(r => r.ResponsibilityType)
                    .Distinct()
                    .ToArray();

                return new ManualOverviewItemViewModel
                {
                    Id = software.Id,
                    Ad = software.Name,
                    BirimAdi = software.Department?.Name,
                    Aciklama = software.Description,
                    TeknikKilavuzYetkisi = roller.Contains(SoftwareResponsibilityType.Yazilimci),
                    KullanimKilavuzuYetkisi = roller.Contains(SoftwareResponsibilityType.BirimKullanicisi) ||
                                              roller.Contains(SoftwareResponsibilityType.BirimYetkilisi),
                    TeknikKilavuz = teknik is null ? null : new ManualFileSummaryViewModel
                    {
                        Id = teknik.Id,
                        Baslik = teknik.Title,
                        DosyaYolu = teknik.FilePath,
                        Versiyon = teknik.Version,
                        YuklenmeZamani = teknik.UploadedOn,
                        Yukleyen = teknik.UploadedByUser?.FullName ?? teknik.UploadedByUser?.UserName,
                        DegisimNotu = teknik.ChangeLog
                    },
                    KullanimKilavuzu = kullanim is null ? null : new ManualFileSummaryViewModel
                    {
                        Id = kullanim.Id,
                        Baslik = kullanim.Title,
                        DosyaYolu = kullanim.FilePath,
                        Versiyon = kullanim.Version,
                        YuklenmeZamani = kullanim.UploadedOn,
                        Yukleyen = kullanim.UploadedByUser?.FullName ?? kullanim.UploadedByUser?.UserName,
                        DegisimNotu = kullanim.ChangeLog
                    }
                };
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => item.Ad)
            .ToList();

        return new ManualOverviewViewModel
        {
            Yazilimlar = items
        };
    }

    public async Task<ManualUploadViewModel> GetManualUploadModelAsync(int userId, int? softwareId, CancellationToken cancellationToken = default)
    {
        var yetkiliYazilimIdleri = await GetAuthorizedSoftwareIdsAsync(userId, cancellationToken);

        var softwares = await _context.Softwares
            .Where(s => yetkiliYazilimIdleri.Contains(s.Id))
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new ManualUploadViewModel
        {
            SoftwareId = softwareId ?? softwares.FirstOrDefault()?.Id ?? 0,
            Yazilimlar = softwares
        };
    }

    public async Task<int> SaveManualAsync(ManualUploadViewModel model, int userId, CancellationToken cancellationToken = default)
    {
        var software = await _context.Softwares
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == model.SoftwareId, cancellationToken)
            ?? throw new InvalidOperationException("Yazılım bulunamadı");

        var izinVerilenRoller = model.ManualType == ManualType.TeknikKilavuz
            ? new[] { SoftwareResponsibilityType.Yazilimci }
            : new[] { SoftwareResponsibilityType.BirimKullanicisi, SoftwareResponsibilityType.BirimYetkilisi };

        var yetkiliMi = await _context.SoftwareResponsibilities
            .AnyAsync(r => r.SoftwareId == software.Id
                           && r.UserId == userId
                           && izinVerilenRoller.Contains(r.ResponsibilityType), cancellationToken);

        if (!yetkiliMi)
        {
            throw new InvalidOperationException("Bu yazılım için yetkiniz bulunmuyor.");
        }

        var manual = new SoftwareManual
        {
            Title = model.Title,
            ManualType = model.ManualType,
            FilePath = model.FilePath,
            Version = model.Version,
            ChangeLog = model.ChangeLog,
            SoftwareId = model.SoftwareId,
            UploadedByUserId = userId,
            UploadedOn = DateTime.UtcNow
        };

        _context.SoftwareManuals.Add(manual);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(userId, "Kılavuz Yükleme", nameof(SoftwareManual), manual.Id,
            $"{software.Name} - {manual.ManualType}", cancellationToken);

        var sorumluIdler = await _context.SoftwareResponsibilities
            .Where(r => r.SoftwareId == software.Id)
            .Select(r => r.UserId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var recipients = await _context.UserProfiles
            .Where(u => u.IsActive && sorumluIdler.Contains(u.Id) && u.Id != userId)
            .ToListAsync(cancellationToken);

        await _notificationService.SendAsync(recipients,
            "Kılavuz Yüklendi",
            $"{software.Name} için {manual.ManualType} kılavuzu güncellendi.",
            manual.FilePath,
            cancellationToken);

        return manual.Id;
    }

    private async Task<int[]> GetAuthorizedSoftwareIdsAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.SoftwareResponsibilities
            .Where(r => r.UserId == userId)
            .Select(r => r.SoftwareId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }
}
