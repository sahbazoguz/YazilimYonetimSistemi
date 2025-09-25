using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UludagSoftwareTracking.Services.Interfaces;

namespace UludagSoftwareTracking.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogInformation("SMTP yapılandırması tanımlı değil. '{Recipient}' alıcısına gönderilecek e-posta kuyruğa alınamadı.", to);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(_settings.FromAddress) ? "no-reply@uludag.edu.tr" : _settings.FromAddress!,
                string.IsNullOrWhiteSpace(_settings.FromName) ? "Uludağ Yazılım Takip" : _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(to);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
        if (!string.IsNullOrWhiteSpace(_settings.UserName) && !string.IsNullOrWhiteSpace(_settings.Password))
        {
            client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
            client.EnableSsl = _settings.EnableSsl;
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException ex)
        {
            _logger.LogWarning(ex, "E-posta gönderimi başarısız oldu. Alıcı: {Recipient}", to);
        }
    }
}

public class EmailSettings
{
    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 25;

    public bool EnableSsl { get; set; } = false;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? FromAddress { get; set; }

    public string? FromName { get; set; }
}
