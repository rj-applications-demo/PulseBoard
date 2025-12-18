namespace PulseBoard.Api.Hubs;

public sealed class SubscribeRequest
{
    public required string ProjectKey { get; init; }
    public string Metric { get; init; } = "event.count";
    public string? Dimension { get; init; }
    public string Interval { get; init; } = "60s";
}

public sealed class UnsubscribeRequest
{
    public required string ProjectKey { get; init; }
    public string Metric { get; init; } = "event.count";
    public string? Dimension { get; init; }
    public string Interval { get; init; } = "60s";
}

public sealed class TimeSeriesUpdateMessage
{
    public required string ProjectKey { get; init; }
    public required string Metric { get; init; }
    public required string Interval { get; init; }
    public string? Dimension { get; init; }
    public required IReadOnlyList<TimeSeriesPoint> DataPoints { get; init; }
}

public sealed class TimeSeriesPoint
{
    public required DateTimeOffset Timestamp { get; init; }
    public required long Value { get; init; }
}
