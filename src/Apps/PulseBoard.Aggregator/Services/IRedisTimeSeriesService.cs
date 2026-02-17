using PulseBoard.Contracts.Messaging;

namespace PulseBoard.Aggregator.Services;

public interface IRedisTimeSeriesService
{
    Task IncrementSecondCounterAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string? dimensionKey,
        DateTimeOffset eventTimestamp,
        CancellationToken ct);

    Task RebuildSixtySecondSeriesAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string? dimensionKey,
        CancellationToken ct);

    Task<IReadOnlyList<TimeSeriesDataPoint>> GetTimeSeriesAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string? dimensionKey,
        string interval,
        CancellationToken ct);

    Task UpdateLongIntervalSeriesAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string? dimensionKey,
        string interval,
        IReadOnlyList<TimeSeriesDataPoint> dataPoints,
        CancellationToken ct);
}
