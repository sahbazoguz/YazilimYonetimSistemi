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

public class SoftwareCatalogService : ISoftwareCatalogService
{
    private readonly ApplicationDbContext _context;

    public SoftwareCatalogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SoftwareCatalogViewModel> GetCatalogAsync(string? search, int? departmentId, CancellationToken cancellationToken = default)
    {
        var query = _context.Softwares
            .Include(s => s.Department)
            .Include(s => s.Manuals)
            .Include(s => s.Request)
            .ThenInclude(r => r.Project)
            .Where(s => s.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var aranacak = $"%{search.Trim()}%";

            query = query.Where(s =>
                EF.Functions.Like(s.Name, aranacak) ||
                (s.Description != null && EF.Functions.Like(s.Description, aranacak)));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(s => s.DepartmentId == departmentId.Value);
        }

        var softwares = await query
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = softwares
            .Select(s =>
            {
                var manual = s.Manuals
                    .Where(m => m.ManualType == ManualType.KullanimKilavuzu && !string.IsNullOrWhiteSpace(m.FilePath))
                    .OrderByDescending(m => m.UploadedOn)
                    .FirstOrDefault();

                var manualTitle = manual?.Title;
                var manualPath = manual?.FilePath;

                if (string.IsNullOrWhiteSpace(manualPath))
                {
                    var projectPath = s.Request?.Project?.UserGuidePath;
                    if (!string.IsNullOrWhiteSpace(projectPath))
                    {
                        manualPath = projectPath;
                        manualTitle ??= $"{s.Name} Kullanım Kılavuzu";
                    }
                }

                return new SoftwareCatalogItemViewModel
                {
                    Id = s.Id,
                    Ad = s.Name,
                    Aciklama = s.Description,
                    Kategori = s.Category,
                    BirimAdi = s.Department?.Name,
                    KullanimKilavuzuBaslik = manualTitle,
                    KullanimKilavuzuUrl = manualPath
                };
            })
            .ToArray();

        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new SoftwareCatalogViewModel
        {
            AramaMetni = search,
            BirimId = departmentId,
            Birimler = departments,
            Yazilimlar = softwares,
            Kayitlar = items
        };
    }

    public async Task<SoftwareDetailViewModel?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var software = await _context.Softwares
            .Include(s => s.Department)
            .Include(s => s.Manuals)
            .ThenInclude(m => m.UploadedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (software is null)
        {
            return null;
        }

        return new SoftwareDetailViewModel
        {
            Yazilim = software,
            Kilavuzlar = software.Manuals
                .OrderByDescending(m => m.UploadedOn)
                .ToArray()
        };
    }

    public async Task<IReadOnlyList<SoftwareManual>> GetManualsForSoftwareAsync(int softwareId, CancellationToken cancellationToken = default)
    {
        return await _context.SoftwareManuals
            .Where(m => m.SoftwareId == softwareId)
            .Include(m => m.UploadedByUser)
            .OrderByDescending(m => m.UploadedOn)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
