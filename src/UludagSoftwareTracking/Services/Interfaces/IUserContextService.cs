using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Services.Interfaces;

public interface IUserContextService
{
    ClaimsPrincipal? Principal { get; }

    string? GetUserName();

    Task<UserProfile?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<bool> IsInRoleAsync(string role, CancellationToken cancellationToken = default);
}
