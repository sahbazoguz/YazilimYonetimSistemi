namespace YazilimYonetimSistemi.Web.Options;

public class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool UseLdap { get; set; }

    public bool AllowPasswordFallback { get; set; } = true;
}
