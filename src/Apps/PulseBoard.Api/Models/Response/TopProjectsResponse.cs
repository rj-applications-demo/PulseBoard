namespace PulseBoard.Api.Models.Response;

public sealed class TopProjectsResponse
{
    public required string Metric { get; init; }
    public required string Interval { get; init; }
    public string? DimensionKey { get; init; }
    public required string Source { get; init; }
    public required IReadOnlyList<ProjectMetric> Projects { get; init; }
}

public sealed class ProjectMetric
{
    public required string ProjectKey { get; init; }
    public required long Value { get; init; }
}
