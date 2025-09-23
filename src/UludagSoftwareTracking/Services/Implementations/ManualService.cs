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

    public ManualService(ApplicationDbContext context)
    {
        _context = context;
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
        var softwareExists = await _context.Softwares.AnyAsync(s => s.Id == model.SoftwareId, cancellationToken);
        if (!softwareExists)
        {
            throw new InvalidOperationException("Yazılım bulunamadı");
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
        return manual.Id;
    }
}
