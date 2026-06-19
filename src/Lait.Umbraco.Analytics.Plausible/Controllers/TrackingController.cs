using Lait.Umbraco.Analytics.Plausible.Models;
using Lait.Umbraco.Analytics.Plausible.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace Lait.Umbraco.Analytics.Plausible.Controllers;

/// <summary>
/// Backoffice Management API. Routes are exposed under
/// /umbraco/management/api/v1/lait-tracking/... and require an authenticated backoffice user.
/// </summary>
[ApiController]
[VersionedApiBackOfficeRoute("lait-tracking")]
[ApiExplorerSettings(GroupName = "Lait Tracking")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
public class TrackingController : ManagementApiControllerBase
{
    private readonly ITrackingService _service;

    public TrackingController(ITrackingService service) => _service = service;

    /// <summary>Aggregate site stats for the dashboard.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(TrackingStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> Stats(string period = "7d", CancellationToken ct = default)
        => Ok(await _service.GetStatsAsync(period, ct));

    /// <summary>Per-page stats for a single document (by its key/unique id).</summary>
    [HttpGet("page")]
    [ProducesResponseType(typeof(PageTrackingStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> Page([FromQuery] Guid id, string period = "7d", CancellationToken ct = default)
        => Ok(await _service.GetPageStatsAsync(id, period, ct));
}
