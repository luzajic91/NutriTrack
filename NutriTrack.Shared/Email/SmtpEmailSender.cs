using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace NutriTrack.Shared.Email;

/// <summary>SMTP implementation of <see cref="IEmailSender"/> backed by MailKit.</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        var socketOptions = _options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, ct);

        if (!string.IsNullOrEmpty(_options.User))
            await client.AuthenticateAsync(_options.User, _options.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);

        _logger.LogInformation("Sent email to {Recipient} with subject {Subject}", to, subject);
    }
}
