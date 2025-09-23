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

public class UserProfileService : IUserProfileService
{
    private readonly ApplicationDbContext _context;

    public UserProfileService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return await _context.UserProfiles
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
    }

    public async Task<UserProfile> EnsureProfileAsync(string userName, string fullName, string? email, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        if (existing is not null)
        {
            existing.FullName = fullName;
            existing.Email = email;
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var profile = new UserProfile
        {
            UserName = userName,
            FullName = string.IsNullOrWhiteSpace(fullName) ? userName : fullName,
            Email = email,
            Role = UserRole.Personel,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .Include(u => u.Department)
            .OrderBy(u => u.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateRoleAsync(string userName, UserRole role, int? departmentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("Kullanıcı adı zorunludur", nameof(userName));
        }

        var profile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException($"{userName} kullanıcısı bulunamadı");
        }

        profile.Role = role;
        profile.DepartmentId = departmentId;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
