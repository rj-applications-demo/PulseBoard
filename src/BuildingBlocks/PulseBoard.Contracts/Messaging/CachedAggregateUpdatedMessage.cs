namespace PulseBoard.Contracts.Messaging;

public sealed class CachedAggregateUpdatedMessage
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required string ProjectKey { get; init; }
    public required string Metric { get; init; }
    public string? DimensionKey { get; init; }
    public required IReadOnlyList<string> UpdatedIntervals { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<TimeSeriesDataPoint>> TimeSeries { get; init; }
}
