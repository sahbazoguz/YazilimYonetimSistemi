using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface IAuditLogService
{
    Task RecordAsync(int userId, string actionType, string entityType, int? entityId, string? description, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActionLog>> GetLogsAsync(CancellationToken cancellationToken = default);
}
