namespace YazilimYonetimSistemi.Web.Options;

public class LdapOptions
{
    public const string SectionName = "Ldap";

    public bool Enabled { get; set; }

    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 389;

    public bool UseSsl { get; set; }

    public string? Domain { get; set; }

    public string BaseDn { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public bool IgnoreCertificateErrors { get; set; }
}
