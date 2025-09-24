namespace YazilimYonetimSistemi.Web.Services;

public interface ILdapAuthenticationService
{
    Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
}
