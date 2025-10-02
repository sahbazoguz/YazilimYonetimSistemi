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

    public async Task<IReadOnlyList<SoftwareManual>> GetManualsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var yetkiliYazilimIdleri = await GetAuthorizedSoftwareIdsAsync(userId, cancellationToken);

        if (yetkiliYazilimIdleri.Length == 0)
        {
            return Array.Empty<SoftwareManual>();
        }

        return await _context.SoftwareManuals
            .Where(m => yetkiliYazilimIdleri.Contains(m.SoftwareId))
            .Include(m => m.Software)
            .Include(m => m.UploadedByUser)
            .OrderByDescending(m => m.UploadedOn)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
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
