using ScholarNotify.Models;
using System.Globalization;

namespace ScholarNotify.ViewModels;

public sealed class DashboardViewModel
{
    public IReadOnlyList<Monitor> Monitors { get; init; } = [];
    public IReadOnlyList<Activity> Activities { get; init; } = [];
    public bool SmsConfigured { get; init; }
    public MonitorInput Input { get; init; } = new();

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

    public static bool TrySplitTimestamp(
        string message,
        out string prefix,
        out DateTimeOffset timestamp,
        out string suffix)
    {
        const string separator = " until ";
        var timestampStart = message.IndexOf(separator, StringComparison.Ordinal);
        if (timestampStart < 0)
        {
            prefix = message;
            timestamp = default;
            suffix = string.Empty;
            return false;
        }

        timestampStart += separator.Length;
        var timestampEnd = message.IndexOf(';', timestampStart);
        if (timestampEnd < 0 && message.EndsWith(".", StringComparison.Ordinal))
        {
            timestampEnd = message.Length - 1;
        }

        timestamp = default;
        if (timestampEnd < 0
            || !DateTimeOffset.TryParse(
                message[timestampStart..timestampEnd],
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out timestamp))
        {
            prefix = message;
            suffix = string.Empty;
            return false;
        }

        prefix = message[..timestampStart];
        suffix = message[timestampEnd..];
        return true;
    }
}