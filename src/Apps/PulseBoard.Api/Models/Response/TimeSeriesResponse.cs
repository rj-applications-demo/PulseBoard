namespace PulseBoard.Api.Models.Response;

public sealed class TimeSeriesResponse
{
    public required string ProjectKey { get; init; }
    public required string Metric { get; init; }
    public required string Interval { get; init; }
    public string? DimensionKey { get; init; }
    public required string Source { get; init; }
    public required IReadOnlyList<TimeSeriesPoint> DataPoints { get; init; }
}

public sealed class TimeSeriesPoint
{
    public required DateTimeOffset Timestamp { get; init; }
    public required long Value { get; init; }
}
