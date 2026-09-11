using Microsoft.EntityFrameworkCore;
using ScholarNotify.Models;

namespace ScholarNotify.Data;

public sealed class ScholarDbContext(DbContextOptions<ScholarDbContext> options) : DbContext(options)
{
    public DbSet<Monitor> Monitors => Set<Monitor>();
    public DbSet<Activity> Activities => Set<Activity>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateNotificationWindows();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateNotificationWindows();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScholarDbContext).Assembly);
    }

    private void ValidateNotificationWindows()
    {
        foreach (var monitor in ChangeTracker.Entries<Monitor>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity))
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
}