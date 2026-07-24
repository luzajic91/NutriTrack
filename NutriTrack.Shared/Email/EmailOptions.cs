namespace NutriTrack.Shared.Email;

/// <summary>SMTP settings, bound from the "Email" configuration section.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@nutritrack.local";
    public string FromName { get; set; } = "NutriTrack";
    public bool UseStartTls { get; set; }
}
