using YazilimYonetimSistemi.Web.Models;

namespace YazilimYonetimSistemi.Web.Services;

public interface IUserAuthenticationService
{
    Task<UserProfile?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
}
