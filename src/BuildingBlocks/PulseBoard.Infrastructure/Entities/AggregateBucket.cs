namespace PulseBoard.Infrastructure.Entities;

public sealed class AggregateBucket
{
    public long Id { get; private set; }

    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }

    public required string Metric { get; init; }
    public required string Interval { get; init; }
    public required DateTimeOffset BucketStartUtc { get; init; }

    public string? DimensionKey { get; init; }

    public long Value { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public Project Project { get; private set; } = null!;
}
