using PulseBoard.Infrastructure.Entities;

namespace PulseBoard.Domain.Events.Entities;

public sealed class Project
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required Guid TenantId { get; init; }
    public required string Key { get; init; }
    public required string Name { get; init; }
    public long TotalEventCount { get; set; }
    public DateTimeOffset CreatedUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastEventUtc { get; set; }

    public Tenant Tenant { get; private set; } = null!;
    public ICollection<EventRecord> Events { get; private set; } = [];
}

