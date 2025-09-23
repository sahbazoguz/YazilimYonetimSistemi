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

    public async Task<SoftwareCatalogViewModel> GetCatalogAsync(string? search, int? departmentId, string? technology, CancellationToken cancellationToken = default)
    {
        var query = _context.Softwares
            .Include(s => s.Department)
            .Where(s => s.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.Contains(search) || (s.Description != null && s.Description.Contains(search)));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(s => s.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(technology))
        {
            query = query.Where(s => s.TechnologyStack != null && s.TechnologyStack.Contains(technology));
        }

        var softwares = await query
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var technologyValues = await _context.Softwares
            .Where(s => !string.IsNullOrEmpty(s.TechnologyStack))
            .Select(s => s.TechnologyStack!)
            .ToListAsync(cancellationToken);

        var technologies = technologyValues
            .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SoftwareCatalogViewModel
        {
            AramaMetni = search,
            BirimId = departmentId,
            Teknoloji = technology,
            Yazilimlar = softwares,
            Birimler = departments,
            Teknolojiler = technologies
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
