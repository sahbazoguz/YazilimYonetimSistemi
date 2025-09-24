using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.Protocols;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly IUserProfileService _userProfileService;
    private readonly ILogger<AccountController> _logger;
    private readonly IConfiguration _configuration;

    public AccountController(IUserProfileService userProfileService, ILogger<AccountController> logger, IConfiguration configuration)
    {
        _userProfileService = userProfileService;
        _logger = logger;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Giris(string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedUserName = NormalizeUserName(model.KullaniciAdi);
        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        var profile = await _userProfileService.GetByUserNameAsync(normalizedUserName)
                      ?? await _userProfileService.EnsureProfileAsync(normalizedUserName, normalizedUserName, null);

        var ldapSuccess = TryLdapAuthentication(normalizedUserName, model.Sifre);
        var passwordSuccess = ldapSuccess || VerifyPassword(model.Sifre, profile.PasswordHash);

        if (!passwordSuccess)
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, profile.UserName),
            new(ClaimTypes.GivenName, string.IsNullOrWhiteSpace(profile.FullName) ? profile.UserName : profile.FullName)
        };

        if (!string.IsNullOrWhiteSpace(profile.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, profile.Email));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = model.BeniHatirla,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cikis()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Giris));
    }

    private string NormalizeUserName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim();
        if (trimmed.Contains("\\", StringComparison.Ordinal) || trimmed.Contains("@", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var defaultDomain = _configuration["Authentication:DefaultDomain"] ?? "ULUDAG";
        return string.IsNullOrWhiteSpace(defaultDomain) ? trimmed : $"{defaultDomain}\\{trimmed}";
    }

    private bool TryLdapAuthentication(string userName, string password)
    {
        var server = _configuration["Ldap:Server"];
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        try
        {
            var useSsl = bool.TryParse(_configuration["Ldap:UseSsl"], out var sslEnabled) && sslEnabled;
            var port = int.TryParse(_configuration["Ldap:Port"], out var configuredPort)
                ? configuredPort
                : useSsl ? 636 : 389;

            using var connection = new LdapConnection(new LdapDirectoryIdentifier(server, port));
            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.SecureSocketLayer = useSsl;
            connection.AuthType = AuthType.Negotiate;

            var credential = new NetworkCredential(userName, password);
            connection.Bind(credential);

            _logger.LogInformation("LDAP kimlik doğrulaması başarılı: {User}", userName);
            return true;
        }
        catch (LdapException ex)
        {
            _logger.LogWarning(ex, "LDAP kimlik doğrulaması başarısız: {User}", userName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LDAP kimlik doğrulaması sırasında beklenmeyen hata: {User}", userName);
            return false;
        }
    }

    private static bool VerifyPassword(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var hash = Convert.FromBase64String(parts[2]);

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var computed = deriveBytes.GetBytes(hash.Length);
        return CryptographicOperations.FixedTimeEquals(computed, hash);
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = string.Empty;

        [Display(Name = "Beni Hatırla")]
        public bool BeniHatirla { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
