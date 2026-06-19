namespace Lait.Umbraco.Analytics.Plausible.Configuration;

/// <summary>
/// Strongly typed configuration bound from the "Lait:Tracking:Plausible" section of appsettings.
/// </summary>
public sealed class PlausibleOptions
{
    public const string SectionName = "Lait:Tracking:Plausible";

    /// <summary>Base URL of the Plausible instance. Use https://plausible.io for Plausible Cloud,
    /// or your own URL for a self-hosted Community Edition instance.</summary>
    public string BaseUrl { get; set; } = "https://plausible.io";

    /// <summary>The Plausible site id (domain), e.g. "example.com".</summary>
    public string SiteId { get; set; } = string.Empty;

    /// <summary>A Plausible "Stats API" key. Keep this server-side (user-secrets / environment).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>How long (seconds) to cache Plausible responses to stay within rate limits.</summary>
    public int CacheSeconds { get; set; } = 120;

    /// <summary>How many top pages to request for the dashboard.</summary>
    public int TopPagesLimit { get; set; } = 5;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SiteId) && !string.IsNullOrWhiteSpace(ApiKey);
}
