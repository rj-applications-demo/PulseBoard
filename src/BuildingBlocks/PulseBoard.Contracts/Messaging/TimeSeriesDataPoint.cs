namespace PulseBoard.Contracts.Messaging;

public sealed class TimeSeriesDataPoint
{
    public required DateTimeOffset Timestamp { get; init; }
    public required long Value { get; init; }
}
