namespace NutriTrack.Shared.Email;

/// <summary>
/// Sends transactional email. Abstracted so the transport (SMTP today) can be
/// swapped without touching feature code.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
