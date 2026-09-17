namespace Identity.Infrastructure.Settings;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 2525;
    public string FromAddress { get; set; } = "noreply@pointbreaklinks.local";
    public string FromName { get; set; } = "PointbreakLinks";
}
