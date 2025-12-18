using System.Text.Json;

namespace PulseBoard.Api.Models.Request;

public sealed class IncomingEventDto
{
    public required string EventId { get; init; }
    public required string ProjectKey { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public JsonElement? Payload { get; init; }
    public IReadOnlyList<DimensionDto>? Dimensions { get; init; }
}

public sealed class DimensionDto
{
    public required string Key { get; init; }
    public required string Value { get; init; }
}
