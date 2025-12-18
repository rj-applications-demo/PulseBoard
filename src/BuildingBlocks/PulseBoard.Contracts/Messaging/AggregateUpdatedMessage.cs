namespace PulseBoard.Contracts.Messaging;

public sealed class AggregateUpdatedMessage
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required string ProjectKey { get; init; }
    public required string Metric { get; init; }
    public required string Interval { get; init; }
    public required DateTimeOffset BucketStartUtc { get; init; }
    public string? DimensionKey { get; init; }
    public required DateTimeOffset EventTimestampUtc { get; init; }
}
