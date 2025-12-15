using PulseBoard.Domain.Events.Entities;

namespace PulseBoard.Infrastructure.Entities;

public sealed class EventRecord
{
    public long Id { get; private set; }

    public required Guid TenantId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid ProjectId { get; init; }

    public required string ProjectKey { get; init; }

    public DateTimeOffset TimestampUtc { get; init; }
    public string? PayloadJson { get; init; }

    public DateTimeOffset CreatedUtc { get; private set; } = DateTimeOffset.UtcNow;

    public Tenant Tenant { get; private set; } = null!;
    public Project Project { get; private set; } = null!;
}
