using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;
using ScholarNotify.Interfaces;
using ScholarNotify.Models;

namespace ScholarNotify.Services;

public sealed partial class ScholarService(IHttpClientFactory httpClientFactory) : IApplicationService
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "scholar.google.com",
        "scholar.googleusercontent.com"
    };

    public async Task<ScholarSnapshot> FetchProfileAsync(string value, CancellationToken cancellationToken = default)
    {
        var (canonicalUrl, userId) = NormalizeUrl(value);
        var httpClient = httpClientFactory.CreateClient("Scholar");
        using var response = await httpClient.GetAsync(canonicalUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google Scholar returned HTTP {(int)response.StatusCode}.");
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (CaptchaRegex().IsMatch(html))
        {
            throw new InvalidOperationException("Google Scholar requested a CAPTCHA. Wait before checking again or use a different server IP.");
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        var name = HtmlEntity.DeEntitize(document.GetElementbyId("gsc_prf_in")?.InnerText ?? string.Empty).Trim();
        var citationsText = document.DocumentNode.SelectSingleNode("//table[@id='gsc_rsb_st']//tbody/tr[1]/td[contains(@class,'gsc_rsb_std')][1]")?.InnerText ?? string.Empty;
        var digits = NonDigitRegex().Replace(citationsText, string.Empty);
        if (string.IsNullOrWhiteSpace(name) || !int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var citations))
        {
            throw new InvalidOperationException("Could not read this profile. Confirm that it is public and try again later.");
        }

        return new ScholarSnapshot(canonicalUrl, userId, name, citations);
    }

    private static (string CanonicalUrl, string UserId) NormalizeUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !AllowedHosts.Contains(uri.Host) || uri.AbsolutePath != "/citations")
        {
            throw new InvalidOperationException("The URL must be a public Google Scholar citations profile.");
        }

        var query = QueryHelpers.ParseQuery(uri.Query);
        var userId = query.TryGetValue("user", out var values) ? values.FirstOrDefault()?.Trim() : null;
        if (string.IsNullOrWhiteSpace(userId) || !UserIdRegex().IsMatch(userId))
        {
            throw new InvalidOperationException("The Scholar URL is missing a valid user ID.");
        }

        return ($"https://scholar.google.com/citations?user={Uri.EscapeDataString(userId)}&hl=en", userId);
    }

    [GeneratedRegex("not a robot|unusual traffic|captcha", RegexOptions.IgnoreCase)]
    private static partial Regex CaptchaRegex();

    [GeneratedRegex("[^0-9]")]
    private static partial Regex NonDigitRegex();

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex UserIdRegex();
}