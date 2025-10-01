using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UludagSoftwareTracking.Data;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Services.Implementations;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;

    public DepartmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .Include(d => d.Users)
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Department> CreateAsync(Department department, CancellationToken cancellationToken = default)
    {
        if (department is null)
        {
            throw new ArgumentNullException(nameof(department));
        }

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);
        return department;
    }

    public async Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .Include(d => d.Users)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Department department, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Departments.FirstOrDefaultAsync(d => d.Id == department.Id, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"{department.Id} numaralı birim bulunamadı");
        }

        existing.Name = department.Name;
        existing.Description = department.Description;
        existing.ContactEmail = department.ContactEmail;
        existing.PhoneNumber = department.PhoneNumber;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (department is null)
        {
            return;
        }

        var hasUsers = await _context.UserProfiles.AnyAsync(u => u.DepartmentId == id, cancellationToken);
        var hasRequests = await _context.SoftwareRequests.AnyAsync(r => r.DepartmentId == id, cancellationToken);
        var hasSoftwares = await _context.Softwares.AnyAsync(s => s.DepartmentId == id, cancellationToken);

        if (hasUsers || hasRequests || hasSoftwares)
        {
            throw new InvalidOperationException("Birim ile ilişkili kayıtlar bulunduğu için silinemiyor.");
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
