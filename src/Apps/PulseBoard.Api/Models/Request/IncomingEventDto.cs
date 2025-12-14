namespace PulseBoard.Api.Models.Request;

public sealed class IncomingEventDto
{
    public required string EventId { get; init; }
    public required string TenantId { get; init; }
    public required string ProjectKey { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? Payload { get; init; }   
}
