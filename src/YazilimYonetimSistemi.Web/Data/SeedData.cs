using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YazilimYonetimSistemi.Web.Models;
using YazilimYonetimSistemi.Web.Services;

namespace YazilimYonetimSistemi.Web.Data;

public static class SeedData
{
    private const string DefaultAdminEmail = "admin@example.com";
    private const string DefaultAdminPassword = "ChangeMe!123";

    public static async Task EnsureSeedDataAsync(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var context = scopedServices.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scopedServices.GetRequiredService<IPasswordHashingService>();
        var logger = scopedServices.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        await context.Database.EnsureCreatedAsync();

        if (await context.UserProfiles.AnyAsync())
        {
            return;
        }

        var adminProfile = new UserProfile
        {
            Email = DefaultAdminEmail,
            NormalizedEmail = DefaultAdminEmail.ToUpperInvariant(),
            DisplayName = "Sistem Yöneticisi",
            Role = UserRoles.Admin,
            AuthenticationProvider = AuthenticationProvider.Internal,
            PasswordHash = passwordHasher.Hash(DefaultAdminPassword),
            IsActive = true
        };

        context.UserProfiles.Add(adminProfile);
        await context.SaveChangesAsync();

        logger.LogWarning("Varsayılan admin kullanıcısı {Email} oluşturuldu. İlk girişte şifrenin değiştirilmesi önerilir.", DefaultAdminEmail);
    }
}
