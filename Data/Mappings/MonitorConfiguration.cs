using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ScholarNotify.Data.Mappings;

public sealed class MonitorConfiguration : IEntityTypeConfiguration<Monitor>
{
    public void Configure(EntityTypeBuilder<Monitor> entity)
    {
        entity.ToTable("monitors");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        entity.Property(item => item.ScholarUrl).HasColumnName("scholar_url").HasMaxLength(2048).IsRequired();
        entity.Property(item => item.ScholarName).HasColumnName("scholar_name").HasMaxLength(512).IsRequired();
        entity.Property(item => item.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32).IsRequired();
        entity.Property(item => item.CurrentCitations).HasColumnName("current_citations");
        entity.Property(item => item.NotifiedCitations).HasColumnName("notified_citations");
        entity.Property(item => item.UtcOffsetHours).HasColumnName("utc_offset_hours");
        entity.Property(item => item.NotificationStartHour).HasColumnName("notification_start_hour");
        entity.Property(item => item.NotificationEndHour).HasColumnName("notification_end_hour");
        entity.Property(item => item.LastCheckedAt).HasColumnName("last_checked_at");
        entity.Property(item => item.NextCheckAt).HasColumnName("next_check_at");
        entity.Property(item => item.LastError).HasColumnName("last_error").HasMaxLength(4000);
        entity.Property(item => item.CreatedAt).HasColumnName("created_at");
        entity.HasIndex(item => item.ScholarUrl).IsUnique().HasDatabaseName("ux_monitors_scholar_url");
    }
}