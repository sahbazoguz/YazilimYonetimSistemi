using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using UludagSoftwareTracking.Extensions;
using UludagSoftwareTracking.Models.Entities;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Services.Implementations;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserProfileService _userProfileService;

    public UserContextService(IHttpContextAccessor httpContextAccessor, IUserProfileService userProfileService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userProfileService = userProfileService;
    }

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string? GetUserName()
    {
        var principal = Principal;
        if (principal?.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        return principal.Identity?.Name;
    }

    public async Task<UserProfile?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userName = GetUserName();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return await _userProfileService.GetByUserNameAsync(userName, cancellationToken);
    }

    public async Task<bool> IsInRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        var principal = Principal;
        if (principal?.IsInRole(role) == true)
        {
            return true;
        }

        var profile = await GetCurrentUserAsync(cancellationToken);
        return profile is not null && string.Equals(profile.Role.ToDisplayName(), role, StringComparison.OrdinalIgnoreCase);
    }
}
