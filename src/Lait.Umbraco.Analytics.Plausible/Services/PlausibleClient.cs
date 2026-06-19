using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lait.Umbraco.Analytics.Plausible.Configuration;
using Microsoft.Extensions.Options;

namespace Lait.Umbraco.Analytics.Plausible.Services;

/// <summary>
/// Thin typed HttpClient over the Plausible Stats API v2 (POST /api/v2/query).
/// Authentication is a Bearer API key, attached here and never exposed to the browser.
/// </summary>
public sealed class PlausibleClient
{
    private readonly HttpClient _http;

    public PlausibleClient(HttpClient http, IOptions<PlausibleOptions> options)
    {
        var opts = options.Value;
        _http = http;

        if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
        {
            _http.BaseAddress = new Uri(opts.BaseUrl, UriKind.Absolute);
        }

        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            // Trim to guard against a stray space/newline pasted into config or user-secrets.
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", opts.ApiKey.Trim());
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Runs a Stats API v2 query. The body is serialized as-is to JSON.</summary>
    public async Task<PlausibleQueryResponse?> QueryAsync(object body, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync("/api/v2/query", body, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            // Surface the real Plausible error (e.g. bad metric, wrong site_id, auth) instead of a bare 500.
            throw new PlausibleApiException((int)response.StatusCode, content);
        }

        return JsonSerializer.Deserialize<PlausibleQueryResponse>(content, JsonOpts);
    }
}

/// <summary>Thrown when Plausible returns a non-success status. Carries the response body for diagnostics.</summary>
public sealed class PlausibleApiException(int statusCode, string body)
    : Exception($"Plausible API returned {statusCode}: {body}")
{
    public int StatusCode { get; } = statusCode;
    public string Body { get; } = body;
}

/// <summary>Shape of a Plausible Stats API v2 query response.</summary>
public sealed class PlausibleQueryResponse
{
    [JsonPropertyName("results")]
    public List<PlausibleResultRow> Results { get; set; } = new();
}

public sealed class PlausibleResultRow
{
    /// <summary>Metric values, in the same order they were requested. Values can be null (e.g. bounce_rate with no data).</summary>
    [JsonPropertyName("metrics")]
    public List<double?> Metrics { get; set; } = new();

    /// <summary>Dimension values (e.g. the page path), in the order requested.</summary>
    [JsonPropertyName("dimensions")]
    public List<string> Dimensions { get; set; } = new();
}
