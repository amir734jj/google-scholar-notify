using ScholarNotify.Models;

namespace ScholarNotify.Services;

public static class NotificationWindow
{
    public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    public static bool IsOpen(Monitor monitor, DateTimeOffset utcNow)
    {
        Validate(monitor);
        var localTime = utcNow.ToOffset(TimeSpan.FromHours(monitor.UtcOffsetHours));
        return localTime.Hour >= monitor.NotificationStartHour
            && localTime.Hour < monitor.NotificationEndHour;
    }

    public static DateTimeOffset GetNextOpening(Monitor monitor, DateTimeOffset utcNow)
    {
        Validate(monitor);
        var offset = TimeSpan.FromHours(monitor.UtcOffsetHours);
        var localNow = utcNow.ToOffset(offset);
        var opening = localNow.Date.AddHours(monitor.NotificationStartHour);
        if (localNow.DateTime >= opening)
        {
            opening = opening.AddDays(1);
        }

        return new DateTimeOffset(opening, offset).ToUniversalTime();
    }

    public static DateTimeOffset GetNextCheck(Monitor monitor, DateTimeOffset utcNow)
    {
        Validate(monitor);
        if (!IsOpen(monitor, utcNow))
        {
            return GetNextOpening(monitor, utcNow);
        }

        var offset = TimeSpan.FromHours(monitor.UtcOffsetHours);
        var localNow = utcNow.ToOffset(offset);
        var candidate = localNow.Add(CheckInterval);
        var closing = localNow.Date.AddHours(monitor.NotificationEndHour);
        return candidate.DateTime < closing
            ? candidate.ToUniversalTime()
            : GetNextOpening(monitor, utcNow);
    }

    private static void Validate(Monitor monitor)
    {
        if (monitor.UtcOffsetHours is < -12 or > 14)
        {
            throw new InvalidOperationException("UTC offset must be between -12 and +14 hours.");
        }

        if (monitor.NotificationStartHour is < 0 or > 23
            || monitor.NotificationEndHour is < 1 or > 24
            || monitor.NotificationStartHour >= monitor.NotificationEndHour)
        {
            throw new InvalidOperationException("Notification start time must be before the end time.");
        }
    }
}