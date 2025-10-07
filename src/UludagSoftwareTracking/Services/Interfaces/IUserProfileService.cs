using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface IUserProfileService
{
    Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    Task<UserProfile> EnsureProfileAsync(string userName, string fullName, string? email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    Task UpdateRoleAsync(string userName, UserRole role, int? departmentId, CancellationToken cancellationToken = default);
}
