using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using PulseBoard.Api.Auth;
using PulseBoard.Api.Services;
using PulseBoard.Infrastructure;

namespace PulseBoard.Api.Controllers;

[ApiController]
[Route("api/metrics")]
public sealed class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly AppDbContext _db;

    public MetricsController(IMetricsService metricsService, AppDbContext db)
    {
        _metricsService = metricsService;
        _db = db;
    }

    [HttpGet("timeseries")]
    public async Task<IActionResult> GetTimeSeries(
        [FromQuery] string projectKey,
        [FromQuery] string metric = "event.count",
        [FromQuery] string interval = "60s",
        [FromQuery] string? dimension = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out StringValues apiKeyHeader) ||
            string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            return Unauthorized(new { error = "Missing X-Api-Key header." });
        }

        Guid? tenantId = await ApiKeyAuthenticator.TryResolveTenantIdAsync(_db, apiKeyHeader!, ct).ConfigureAwait(false);
        if (tenantId is null)
            return Unauthorized(new { error = "Invalid API key." });

        if (string.IsNullOrWhiteSpace(projectKey))
            return BadRequest(new { error = "projectKey is required." });

        if (!IsValidInterval(interval))
            return BadRequest(new { error = "Invalid interval. Allowed values: 60s, 60m, 24h." });

        var result = await _metricsService.GetTimeSeriesAsync(
            tenantId.Value,
            projectKey.Trim(),
            metric,
            interval,
            dimension,
            from,
            to,
            ct).ConfigureAwait(false);

        if (result is null)
            return NotFound(new { error = "Project not found." });

        return Ok(result);
    }

    [HttpGet("top")]
    public async Task<IActionResult> GetTopProjects(
        [FromQuery] string metric = "event.count",
        [FromQuery] string interval = "60m",
        [FromQuery] string? dimension = null,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out StringValues apiKeyHeader) ||
            string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            return Unauthorized(new { error = "Missing X-Api-Key header." });
        }

        Guid? tenantId = await ApiKeyAuthenticator.TryResolveTenantIdAsync(_db, apiKeyHeader!, ct).ConfigureAwait(false);
        if (tenantId is null)
            return Unauthorized(new { error = "Invalid API key." });

        if (!IsValidInterval(interval))
            return BadRequest(new { error = "Invalid interval. Allowed values: 60s, 60m, 24h." });

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "Limit must be between 1 and 100." });

        var result = await _metricsService.GetTopProjectsAsync(
            tenantId.Value,
            metric,
            interval,
            dimension,
            limit,
            ct).ConfigureAwait(false);

        return Ok(result);
    }

    private static bool IsValidInterval(string interval)
    {
        return interval is "60s" or "60m" or "24h";
    }
}
