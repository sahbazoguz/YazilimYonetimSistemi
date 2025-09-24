using System.ComponentModel.DataAnnotations;

namespace YazilimYonetimSistemi.Web.Models;

public class UserProfile
{
    private string _email = string.Empty;
    private string _normalizedEmail = string.Empty;

    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email
    {
        get => _email;
        set
        {
            _email = (value ?? string.Empty).Trim();
            _normalizedEmail = _email.ToUpperInvariant();
        }
    }

    [Required]
    [MaxLength(256)]
    public string NormalizedEmail
    {
        get => _normalizedEmail;
        set => _normalizedEmail = (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    [Required]
    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    [Required]
    [MaxLength(64)]
    public string Role { get; set; } = UserRoles.UnitUser;

    [MaxLength(512)]
    public string? PasswordHash { get; set; }

    public AuthenticationProvider AuthenticationProvider { get; set; } = AuthenticationProvider.Ldap;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }
}
