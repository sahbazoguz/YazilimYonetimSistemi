using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using UludagSoftwareTracking.Extensions;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Services.Security;

public class UserClaimsTransformation : IClaimsTransformation
{
    private readonly IUserProfileService _userProfileService;
    private readonly ILogger<UserClaimsTransformation> _logger;

    public UserClaimsTransformation(IUserProfileService userProfileService, ILogger<UserClaimsTransformation> logger)
    {
        _userProfileService = userProfileService;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        var userName = identity.Name;
        if (string.IsNullOrWhiteSpace(userName))
        {
            return principal;
        }

        var profile = await _userProfileService.GetByUserNameAsync(userName) ??
                      await _userProfileService.EnsureProfileAsync(userName, identity.Name ?? userName, identity.FindFirst(ClaimTypes.Email)?.Value);

        _logger.LogDebug("Kullanıcı profili eşleştirildi: {User}", userName);

        if (!identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, profile.Role.ToDisplayName()));
        }

        if (!identity.HasClaim(c => c.Type == ClaimTypes.GivenName) && !string.IsNullOrWhiteSpace(profile.FullName))
        {
            identity.AddClaim(new Claim(ClaimTypes.GivenName, profile.FullName));
        }

        return principal;
    }
}
