using System.Data;
using FluentMigrator;

namespace ScholarNotify.Data.Migrations;

[Migration(2026091101)]
public sealed class InitialSchema : Migration
{
    public override void Up()
    {
        var monitorsExist = Schema.Table("monitors").Exists();
        if (!monitorsExist)
        {
            Create.Table("monitors")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("scholar_url").AsString(2048).NotNullable()
                .WithColumn("scholar_name").AsString(512).NotNullable()
                .WithColumn("phone_number").AsString(32).NotNullable()
                .WithColumn("current_citations").AsInt32().NotNullable()
                .WithColumn("notified_citations").AsInt32().NotNullable()
                .WithColumn("interval_hours").AsInt32().NotNullable()
                .WithColumn("utc_offset_hours").AsInt32().NotNullable().WithDefaultValue(-6)
                .WithColumn("notification_start_hour").AsInt32().NotNullable().WithDefaultValue(9)
                .WithColumn("notification_end_hour").AsInt32().NotNullable().WithDefaultValue(17)
                .WithColumn("enabled").AsBoolean().NotNullable()
                .WithColumn("last_checked_at").AsDateTimeOffset().Nullable()
                .WithColumn("next_check_at").AsDateTimeOffset().NotNullable()
                .WithColumn("last_error").AsString(4000).Nullable()
                .WithColumn("created_at").AsDateTimeOffset().NotNullable();

            Create.Index("ux_monitors_scholar_url")
                .OnTable("monitors")
                .OnColumn("scholar_url")
                .Ascending()
                .WithOptions().Unique();
        }

        if (!Schema.Table("activity").Exists())
        {
            Create.Table("activity")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("monitor_id").AsInt64().NotNullable()
                    .ForeignKey("fk_activity_monitors", "monitors", "id").OnDelete(Rule.Cascade)
                .WithColumn("kind").AsString(64).NotNullable()
                .WithColumn("message").AsString(4000).NotNullable()
                .WithColumn("created_at").AsDateTimeOffset().NotNullable();
        }
    }

    public override void Down()
    {
        if (Schema.Table("activity").Exists())
        {
            Delete.Table("activity");
        }

        if (Schema.Table("monitors").Exists())
        {
            Delete.Table("monitors");
        }
    }
}