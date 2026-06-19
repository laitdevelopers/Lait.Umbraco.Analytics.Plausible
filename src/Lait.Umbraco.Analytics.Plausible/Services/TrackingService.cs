using Lait.Umbraco.Analytics.Plausible.Configuration;
using Lait.Umbraco.Analytics.Plausible.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace Lait.Umbraco.Analytics.Plausible.Services;

public sealed class TrackingService : ITrackingService
{
    private static readonly string[] AllowedPeriods = { "day", "7d", "30d", "month", "6mo", "12mo" };

    private readonly PlausibleClient _client;
    private readonly PlausibleOptions _options;
    private readonly IMemoryCache _cache;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly ILogger<TrackingService> _logger;

    public TrackingService(
        PlausibleClient client,
        IOptions<PlausibleOptions> options,
        IMemoryCache cache,
        IUmbracoContextFactory umbracoContextFactory,
        IPublishedUrlProvider urlProvider,
        ILogger<TrackingService> logger)
    {
        _client = client;
        _options = options.Value;
        _cache = cache;
        _umbracoContextFactory = umbracoContextFactory;
        _urlProvider = urlProvider;
        _logger = logger;
    }

    public async Task<TrackingStats> GetStatsAsync(string period, CancellationToken ct = default)
    {
        period = Normalize(period);

        if (!_options.IsConfigured)
        {
            return new TrackingStats { Configured = false, Period = period, Diagnostics = BuildDiagnostics() };
        }

        var cacheKey = $"lait-tracking:stats:{period}";
        if (_cache.TryGetValue(cacheKey, out TrackingStats? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            // 1) Aggregate metrics for the whole site.
            var aggregate = await _client.QueryAsync(new
            {
                site_id = _options.SiteId,
                metrics = new[] { "visitors", "pageviews", "visits", "bounce_rate" },
                date_range = period,
            }, ct);

            var row = aggregate?.Results.FirstOrDefault();

            // 2) Top pages.
            var topResponse = await _client.QueryAsync(new
            {
                site_id = _options.SiteId,
                metrics = new[] { "visitors" },
                date_range = period,
                dimensions = new[] { "event:page" },
                order_by = new[] { new object[] { "visitors", "desc" } },
                pagination = new { limit = _options.TopPagesLimit },
            }, ct);

            var topPages = (topResponse?.Results ?? new())
                .Where(r => r.Dimensions.Count > 0 && r.Metrics.Count > 0)
                .Select(r => new TopPage { Page = r.Dimensions[0], Visitors = Metric(r, 0) })
                .ToList();

            var stats = new TrackingStats
            {
                Configured = true,
                Period = period,
                Visitors = Metric(row, 0),
                Pageviews = Metric(row, 1),
                Visits = Metric(row, 2),
                BounceRate = row is not null && row.Metrics.Count > 3 ? Math.Round(row.Metrics[3] ?? 0, 1) : 0,
                TopPages = topPages,
                Diagnostics = BuildDiagnostics(),
            };

            _cache.Set(cacheKey, stats, TimeSpan.FromSeconds(_options.CacheSeconds));
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plausible stats query failed for site {SiteId}", _options.SiteId);
            return new TrackingStats
            {
                Configured = true,
                Period = period,
                Diagnostics = BuildDiagnostics(),
                Error = Describe(ex),
            };
        }
    }

    public async Task<PageTrackingStats> GetPageStatsAsync(Guid documentKey, string period, CancellationToken ct = default)
    {
        period = Normalize(period);

        if (!_options.IsConfigured)
        {
            return new PageTrackingStats { Configured = false, Period = period };
        }

        var path = ResolvePath(documentKey);
        if (string.IsNullOrWhiteSpace(path))
        {
            // Unpublished / no URL yet.
            return new PageTrackingStats { Configured = true, Period = period, Path = null };
        }

        var cacheKey = $"lait-tracking:page:{period}:{path}";
        if (_cache.TryGetValue(cacheKey, out PageTrackingStats? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var response = await _client.QueryAsync(new
            {
                site_id = _options.SiteId,
                metrics = new[] { "visitors", "pageviews" },
                date_range = period,
                filters = new object[] { new object[] { "is", "event:page", new[] { path } } },
            }, ct);

            var row = response?.Results.FirstOrDefault();

            var stats = new PageTrackingStats
            {
                Configured = true,
                Period = period,
                Path = path,
                Visitors = Metric(row, 0),
                Pageviews = Metric(row, 1),
            };

            _cache.Set(cacheKey, stats, TimeSpan.FromSeconds(_options.CacheSeconds));
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plausible page query failed for {Path}", path);
            return new PageTrackingStats { Configured = true, Period = period, Path = path, Error = Describe(ex) };
        }
    }

    /// <summary>Resolves an Umbraco document key to its published relative URL path.</summary>
    private string? ResolvePath(Guid documentKey)
    {
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var content = contextReference.UmbracoContext.Content?.GetById(documentKey);
        if (content is null)
        {
            return null;
        }

        var url = _urlProvider.GetUrl(content, UrlMode.Relative);
        if (string.IsNullOrWhiteSpace(url) || url == "#")
        {
            return null;
        }

        // Plausible stores the pathname without a trailing slash variation issue; keep as-is.
        return url;
    }

    private TrackingDiagnostics BuildDiagnostics() => new()
    {
        BaseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl) ? "(not set)" : _options.BaseUrl,
        SiteId = string.IsNullOrWhiteSpace(_options.SiteId) ? "(not set)" : _options.SiteId,
        ApiKeySet = !string.IsNullOrWhiteSpace(_options.ApiKey),
        ApiKeyPreview = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? "(not set)"
            : $"set ({_options.ApiKey.Trim().Length} chars)",
        CacheSeconds = _options.CacheSeconds,
        TopPagesLimit = _options.TopPagesLimit,
    };

    private static int Metric(PlausibleResultRow? row, int index) =>
        row is not null && row.Metrics.Count > index ? (int)(row.Metrics[index] ?? 0) : 0;

    private static string Describe(Exception ex) => ex switch
    {
        PlausibleApiException api => $"Plausible returned HTTP {api.StatusCode}. {Truncate(api.Body)}",
        _ => ex.Message,
    };

    private static string Truncate(string s) =>
        string.IsNullOrEmpty(s) ? string.Empty : (s.Length > 300 ? s[..300] + "…" : s);

    private static string Normalize(string period) =>
        AllowedPeriods.Contains(period, StringComparer.OrdinalIgnoreCase) ? period : "7d";
}
