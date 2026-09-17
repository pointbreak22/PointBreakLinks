namespace Infrastructure.Settings;

public class SiteReverificationSettings
{
    public const string SectionName = "SiteReverification";

    // 24h in production; overridden much lower in tests/manual verification so the job doesn't
    // need a real day of wall-clock time to observe running.
    public int IntervalHours { get; set; } = 24;
}
