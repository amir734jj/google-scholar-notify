FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ScholarNotify.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
RUN apk add --no-cache krb5-libs
COPY --from=build /app/publish .
ENV PORT=3000
EXPOSE 3000
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
	CMD wget -q --spider "http://127.0.0.1:${PORT}/health" || exit 1
ENTRYPOINT ["dotnet", "ScholarNotify.dll"]