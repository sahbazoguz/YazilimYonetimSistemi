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

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecordAsync(int userId, string actionType, string entityType, int? entityId, string? description, CancellationToken cancellationToken = default)
    {
        var log = new ActionLog
        {
            UserId = userId,
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ActionLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActionLog>> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ActionLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
