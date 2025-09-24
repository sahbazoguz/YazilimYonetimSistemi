using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YazilimYonetimSistemi.Web.Options;

namespace YazilimYonetimSistemi.Web.Services;

public class LdapAuthenticationService : ILdapAuthenticationService
{
    private readonly IOptions<LdapOptions> _options;
    private readonly ILogger<LdapAuthenticationService> _logger;

    public LdapAuthenticationService(IOptions<LdapOptions> options, ILogger<LdapAuthenticationService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);

        if (string.IsNullOrEmpty(password))
        {
            return Task.FromResult(false);
        }

        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogDebug("LDAP authentication skipped because it is disabled.");
            return Task.FromResult(false);
        }

        return Task.Run(() => AuthenticateInternal(username, password, options), cancellationToken);
    }

    private bool AuthenticateInternal(string username, string password, LdapOptions options)
    {
        try
        {
            var identifier = new LdapDirectoryIdentifier(options.Server, options.Port);
            using var connection = new LdapConnection(identifier);

            if (options.UseSsl)
            {
                connection.SessionOptions.SecureSocketLayer = true;

                if (options.IgnoreCertificateErrors)
                {
                    connection.SessionOptions.VerifyServerCertificate += (_, _) => true;
                }
            }

            var timeout = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30;
            connection.Timeout = TimeSpan.FromSeconds(timeout);

            var formattedUserName = FormatUsername(username, options);
            var credential = new NetworkCredential(formattedUserName, password);

            connection.Bind(credential);
            _logger.LogInformation("LDAP authentication successful for user {UserName}.", username);
            return true;
        }
        catch (LdapException ldapException)
        {
            _logger.LogWarning(ldapException, "LDAP authentication failed for user {UserName}.", username);
            return false;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error during LDAP authentication for user {UserName}.", username);
            return false;
        }
    }

    private static string FormatUsername(string username, LdapOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Domain) && !username.Contains('@', StringComparison.Ordinal) && !username.Contains('\\'))
        {
            return $"{username}@{options.Domain}";
        }

        return username;
    }
}
