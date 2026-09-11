using EfCoreRepository.Extensions;
using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using ScholarNotify.Data;
using ScholarNotify.Data.Migrations;
using ScholarNotify.Infrastructure;
using ScholarNotify.Interfaces;
using ScholarNotify.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

if (int.TryParse(builder.Configuration["PORT"], out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var useSqlite = builder.Environment.IsDevelopment();
var connectionString = useSqlite
    ? CreateSqliteConnectionString(builder.Configuration)
    : DatabaseUrlConverter.ToConnectionString(
        builder.Configuration["DATABASE_URL"]
        ?? throw new InvalidOperationException("DATABASE_URL is required outside the Development environment."));

builder.Services.AddControllersWithViews().AddNewtonsoftJson();
builder.Services.AddDbContextFactory<ScholarDbContext>(options =>
{
    if (useSqlite)
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ScholarDbContext>("database");
builder.Services.AddFluentMigratorCore().ConfigureRunner(runner =>
{
    if (useSqlite)
    {
        runner.AddSQLite();
    }
    else
    {
        runner.AddPostgres();
    }

    runner.WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(InitialSchema).Assembly).For.Migrations();
});
builder.Services.AddEfRepositoryFactory<ScholarDbContext>(options => options.DefaultProfiles());
builder.Services.AddHttpClient("Scholar", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (compatible; ScholarCitationMonitor/1.0; +https://github.com/amir734jj)");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
});
builder.Services.AddHttpClient("SmsProxyHub", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.Scan(scan => scan
    .FromAssemblyOf<IApplicationService>()
    .AddClasses(classes => classes.AssignableTo<IApplicationService>())
    .AsSelf()
    .WithSingletonLifetime());
builder.Services.AddHostedService<MonitorWorker>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseRouting();
app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
}
app.Run();

static string CreateSqliteConnectionString(IConfiguration configuration)
{
    var dataDirectory = Path.GetFullPath(configuration["DATA_DIR"] ?? configuration["DataDirectory"] ?? "data");
    Directory.CreateDirectory(dataDirectory);
    return $"Data Source={Path.Combine(dataDirectory, "scholar-notify.db")}";
}