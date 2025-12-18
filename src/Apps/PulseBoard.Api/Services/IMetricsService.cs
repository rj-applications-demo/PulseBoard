using PulseBoard.Api.Models.Response;

namespace PulseBoard.Api.Services;

public interface IMetricsService
{
    Task<TimeSeriesResponse?> GetTimeSeriesAsync(
        Guid tenantId,
        string projectKey,
        string metric,
        string interval,
        string? dimensionKey,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken ct);

    Task<TopProjectsResponse> GetTopProjectsAsync(
        Guid tenantId,
        string metric,
        string interval,
        string? dimensionKey,
        int limit,
        CancellationToken ct);
}
