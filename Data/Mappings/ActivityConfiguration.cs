using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarNotify.Models;

namespace ScholarNotify.Data.Mappings;

public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> entity)
    {
        entity.ToTable("activity");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        entity.Property(item => item.MonitorId).HasColumnName("monitor_id");
        entity.Property(item => item.Kind).HasColumnName("kind").HasMaxLength(64).IsRequired();
        entity.Property(item => item.Message).HasColumnName("message").HasMaxLength(4000).IsRequired();
        entity.Property(item => item.CreatedAt).HasColumnName("created_at");
        entity.HasOne(item => item.Monitor)
            .WithMany(item => item.Activities)
            .HasForeignKey(item => item.MonitorId)
            .HasConstraintName("fk_activity_monitors")
            .OnDelete(DeleteBehavior.Cascade);
    }
}