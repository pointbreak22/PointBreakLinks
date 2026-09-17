using System.Net.Mail;
using Application.Common;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

// No real email provider configured anywhere in this project (see PROJECT_MAP.md's "Известные
// ограничения") — points at a local SMTP catcher (smtp4dev) by default. Identity.Infrastructure
// has its own copy of this class for the same reason it has its own copy of everything else
// (see PROJECT_MAP.md's Identity split section) — the two bounded contexts don't share an
// Infrastructure project to depend on together.
public class SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var options = settings.Value;

        using var client = new SmtpClient(options.SmtpHost, options.SmtpPort);
        using var message = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Never let email delivery break the calling command — same reasoning as
            // SignalRNotificationPusher swallowing its own push failures.
            logger.LogWarning(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
