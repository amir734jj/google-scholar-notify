namespace ScholarNotify.Models;

public sealed class Monitor
{
    public long Id { get; set; }
    public string ScholarUrl { get; set; } = string.Empty;
    public string ScholarName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int CurrentCitations { get; set; }
    public int NotifiedCitations { get; set; }
    public int IntervalHours { get; set; }
    public int UtcOffsetHours { get; set; } = -6;
    public int NotificationStartHour { get; set; } = 9;
    public int NotificationEndHour { get; set; } = 17;
    public bool Enabled { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
    public DateTimeOffset NextCheckAt { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Activity> Activities { get; set; } = [];
}