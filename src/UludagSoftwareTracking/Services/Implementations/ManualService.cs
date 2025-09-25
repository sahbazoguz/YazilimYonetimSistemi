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

    public async Task<IReadOnlyList<SoftwareManual>> GetManualsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SoftwareManuals
            .Include(m => m.Software)
            .Include(m => m.UploadedByUser)
            .OrderByDescending(m => m.UploadedOn)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ManualUploadViewModel> GetManualUploadModelAsync(int? softwareId, CancellationToken cancellationToken = default)
    {
        var softwares = await _context.Softwares
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

        var recipients = await _context.UserProfiles
            .Where(u => u.IsActive && u.Id != userId)
            .Where(u =>
                (software.DepartmentId != null && u.DepartmentId == software.DepartmentId &&
                 (u.Role == UserRole.BirimYetkilisi || u.Role == UserRole.BirimKullanicisi)) ||
                u.Role == UserRole.Yazilimci)
            .ToListAsync(cancellationToken);

        await _notificationService.SendAsync(recipients,
            "Kılavuz Yüklendi",
            $"{software.Name} için {manual.ManualType} kılavuzu güncellendi.",
            manual.FilePath,
            cancellationToken);

        return manual.Id;
    }
}
