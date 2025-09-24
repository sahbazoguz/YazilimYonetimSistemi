using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YazilimYonetimSistemi.Web.Data;
using YazilimYonetimSistemi.Web.Models;
using YazilimYonetimSistemi.Web.Options;

namespace YazilimYonetimSistemi.Web.Services;

public class UserAuthenticationService : IUserAuthenticationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILdapAuthenticationService _ldapAuthenticationService;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IOptions<AuthenticationOptions> _authenticationOptions;
    private readonly ILogger<UserAuthenticationService> _logger;

    public UserAuthenticationService(
        ApplicationDbContext dbContext,
        ILdapAuthenticationService ldapAuthenticationService,
        IPasswordHashingService passwordHashingService,
        IOptions<AuthenticationOptions> authenticationOptions,
        ILogger<UserAuthenticationService> logger)
    {
        _dbContext = dbContext;
        _ldapAuthenticationService = ldapAuthenticationService;
        _passwordHashingService = passwordHashingService;
        _authenticationOptions = authenticationOptions;
        _logger = logger;
    }

    public async Task<UserProfile?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await _dbContext.UserProfiles
            .Include(profile => profile.Department)
            .SingleOrDefaultAsync(profile => profile.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Login failed for {Email}: user not found.", email);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogInformation("Login failed for {Email}: user inactive.", email);
            return null;
        }

        var options = _authenticationOptions.Value;
        var ldapAllowed = options.UseLdap && user.AuthenticationProvider == AuthenticationProvider.Ldap;

        if (ldapAllowed)
        {
            var ldapSuccess = await _ldapAuthenticationService.AuthenticateAsync(user.Email, password, cancellationToken);
            if (ldapSuccess)
            {
                user.LastLoginAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return user;
            }

            if (!options.AllowPasswordFallback)
            {
                _logger.LogInformation("LDAP authentication failed for {Email} and password fallback is disabled.", email);
                return null;
            }
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            _logger.LogInformation("User {Email} does not have a local password hash configured.", email);
            return null;
        }

        if (!_passwordHashingService.Verify(user.PasswordHash, password))
        {
            _logger.LogInformation("Password verification failed for {Email}.", email);
            return null;
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}
