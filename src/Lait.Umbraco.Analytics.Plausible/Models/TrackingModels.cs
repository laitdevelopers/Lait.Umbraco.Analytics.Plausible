namespace Lait.Umbraco.Analytics.Plausible.Models;

/// <summary>Aggregate stats for the Content dashboard.</summary>
public sealed class TrackingStats
{
    /// <summary>False when the package has no SiteId/ApiKey configured yet.</summary>
    public bool Configured { get; set; }
    public string Period { get; set; } = "7d";
    public int Visitors { get; set; }
    public int Pageviews { get; set; }
    public int Visits { get; set; }
    public double BounceRate { get; set; }
    public IReadOnlyList<TopPage> TopPages { get; set; } = Array.Empty<TopPage>();

    /// <summary>Non-secret view of the config the server actually resolved (for troubleshooting).</summary>
    public TrackingDiagnostics Diagnostics { get; set; } = new();

    /// <summary>Set when a call to Plausible failed; carries a readable reason for the dashboard.</summary>
    public string? Error { get; set; }
}

/// <summary>What the server can read from configuration. The API key is never returned, only masked.</summary>
public sealed class TrackingDiagnostics
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public bool ApiKeySet { get; set; }

    /// <summary>e.g. "set (40 chars)" or "(not set)" — never the key itself.</summary>
    public string ApiKeyPreview { get; set; } = string.Empty;
    public int CacheSeconds { get; set; }
    public int TopPagesLimit { get; set; }
}

public sealed class TopPage
{
    public string Page { get; set; } = string.Empty;
    public int Visitors { get; set; }
}

/// <summary>Per-page stats for the document Info tab.</summary>
public sealed class PageTrackingStats
{
    public bool Configured { get; set; }
    public string Period { get; set; } = "7d";

    /// <summary>The resolved page path that was queried, e.g. "/about-us/". Null if it could not be resolved.</summary>
    public string? Path { get; set; }

    /// <summary>Total page views in the period.</summary>
    public int Pageviews { get; set; }

    /// <summary>Unique visitors in the period.</summary>
    public int Visitors { get; set; }

    /// <summary>Set when a call to Plausible failed; carries a readable reason.</summary>
    public string? Error { get; set; }
}
