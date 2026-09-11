using ScholarNotify.Models;

namespace ScholarNotify.ViewModels;

public sealed class DashboardViewModel
{
    public IReadOnlyList<Monitor> Monitors { get; init; } = [];
    public IReadOnlyList<Activity> Activities { get; init; } = [];
    public bool SmsConfigured { get; init; }
    public MonitorInput Input { get; init; } = new();

    public static string Initials(string name) => string.Concat(name
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Where(part => !part.TrimEnd('.').Equals("dr", StringComparison.OrdinalIgnoreCase)
            && !part.TrimEnd('.').Equals("prof", StringComparison.OrdinalIgnoreCase))
        .Take(2)
        .Select(part => char.ToUpperInvariant(part[0])));

    public static string RelativeTime(DateTimeOffset value)
    {
        var difference = value - DateTimeOffset.UtcNow;
        var future = difference >= TimeSpan.Zero;
        var duration = difference.Duration();
        var amount = duration.TotalDays >= 1 ? $"{Math.Round(duration.TotalDays):N0}d"
            : duration.TotalHours >= 1 ? $"{Math.Round(duration.TotalHours):N0}h"
            : $"{Math.Max(1, Math.Round(duration.TotalMinutes)):N0}m";
        return future ? $"in {amount}" : $"{amount} ago";
    }
}