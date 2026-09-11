# Citewatch

A .NET 10 web app that monitors public Google Scholar profiles and sends citation-change alerts through [SMS Proxy Hub](https://github.com/amir734jj/sms-proxy-hub).

## Setup

Requires the .NET 10 SDK and a running SMS Proxy Hub connection.

```text
SMS_PROXY_URL=https://sms-proxy-hub.example.com
SMS_PROXY_TOKEN=your-api-token
SMS_CONNECTION_ID=optional-connection-guid
PORT=3000
```

```bash
dotnet run
```

Open `http://localhost:3000`, or the URL printed by ASP.NET.

Register `https://your-citewatch-host/api/sms-callback` as a webhook in SMS Proxy Hub to record SMS status and replies.

## How it works

- Adding a profile stores its current citation count as the baseline. It does not send an initial SMS.
- Profiles are checked every 15 minutes during their configured local notification window.
- An SMS is sent only when the citation count changes.
- Development data is stored in `data/scholar-notify.db`. Set `DATA_DIR` to change the directory.

## Production

Outside `Development`, set a PostgreSQL URL:

```text
DATABASE_URL=postgresql://postgres:change-me@postgres:5432/scholar_notify?sslmode=Prefer
```

```bash
docker build -t citewatch .
docker run --env-file .env -p 3000:3000 citewatch
```

The dashboard has no authentication, so keep it on a trusted network or behind an access proxy. Google Scholar may rate-limit requests or present CAPTCHAs; fetch failures appear in the dashboard without changing the notification baseline.