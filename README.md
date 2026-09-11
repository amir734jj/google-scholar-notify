# Citewatch

A .NET 10 ASP.NET Core MVC application that watches public Google Scholar profiles and sends an SMS through [SMS Proxy Hub](https://github.com/amir734jj/sms-proxy-hub) whenever a total citation count changes. Persistence uses EF Core with SQLite for development, PostgreSQL for production, [SimpleEfCoreRepository](https://github.com/amir734jj/ef-core-repository), and FluentMigrator for schema changes. Application services are registered with Scrutor, and SMS API calls use Refit with Json.NET serialization.

Application and HTTP request logs use Serilog. Console output and minimum levels are configured in the `Serilog` section of `appsettings.json`; existing `ILogger<T>` calls are routed through the same provider.

## Setup

Requirements: .NET 10 SDK and a running SMS Proxy Hub connection.

Configure `SmsProxyHub` in `appsettings.json`, use .NET configuration environment variables such as `SmsProxyHub__BaseUrl`, or use the supported aliases below:

```text
SMS_PROXY_URL=https://sms-proxy-hub.example.com
SMS_PROXY_TOKEN=your-api-token
SMS_CONNECTION_ID=optional-connection-guid
PORT=3000
```

Then restore dependencies and run the app:

```bash
dotnet restore
dotnet run
```

Open the URL printed by ASP.NET. With `PORT=3000`, it is `http://localhost:3000`.

The dashboard has no user authentication. Keep it on a trusted network or place it behind your own access proxy when deploying publicly.

## How it works

Adding a profile performs one immediate fetch and stores the current citation total as its baseline. Phone destinations are validated with `libphonenumber-csharp` and normalized to E.164 before storage and again before SMS delivery. Each profile also has a fixed UTC offset and local delivery window, defaulting to `UTC-06:00` from 09:00 to 17:00. The start hour must always be before the end hour. Citation checks run through ASP.NET Core's built-in `BackgroundService`, registered with `AddHostedService<MonitorWorker>()` and scheduled by `PeriodicTimer`. It scans once per minute for due profiles. A changed total is sent to SMS Proxy Hub using `POST /api/messages/send`; the notified baseline advances only after the hub accepts the message. Changes detected outside the allowed window remain pending and are retried when the next window opens.

This intentionally uses the .NET hosted-service infrastructure instead of Hangfire. The scheduler starts and stops with the web host and does not require a separate Hangfire database or dashboard.

In the `Development` environment, data is stored locally in `data/scholar-notify.db`. Override the directory with `DATA_DIR` when needed.

FluentMigrator applies pending migrations when the application starts. The baseline migration adopts existing databases without recreating their tables, and later migrations apply incremental schema changes.

Outside `Development`, the app uses PostgreSQL and requires `ConnectionStrings:Postgres`. With environment variables, use the standard .NET double-underscore form:

```text
ConnectionStrings__Postgres=Host=postgres;Port=5432;Database=scholar_notify;Username=postgres;Password=change-me
```

Run the production container with that connection string:

```bash
docker build -t citewatch .
docker run --env-file .env -p 3000:3000 citewatch
```

The container uses the .NET 10 Alpine SDK and runtime images and runs as the built-in non-root user. Docker checks `GET /health` every 30 seconds; the endpoint reports unhealthy when the configured database cannot be reached.

## Operational note

Google Scholar does not provide an official profile API and can present CAPTCHAs or rate-limit automated requests. Keep check intervals conservative. Fetch failures appear in the dashboard and never overwrite the notified citation baseline.