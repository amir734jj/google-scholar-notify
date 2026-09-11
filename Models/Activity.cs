namespace ScholarNotify.Models;

public sealed class Activity
{
    public long Id { get; set; }
    public long MonitorId { get; set; }
    public Monitor Monitor { get; set; } = null!;
    public string Kind { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}