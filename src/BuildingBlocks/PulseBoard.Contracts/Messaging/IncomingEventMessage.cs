namespace PulseBoard.Contracts.Messaging;

public sealed class IncomingEventMessage
{
    public required string EventId { get; init; }
    public required string TenantId { get; init; }
    public required string ProjectKey { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? Payload { get; init; }
}
