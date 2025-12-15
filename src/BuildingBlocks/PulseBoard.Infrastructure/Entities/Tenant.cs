using PulseBoard.Domain.Events.Entities;

namespace PulseBoard.Infrastructure.Entities;

public sealed class Tenant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required string Name { get; init; }
    public DateTimeOffset CreatedUtc { get; private set; } = DateTimeOffset.UtcNow;

    public ICollection<User> Users { get; private set; } = [];
    public ICollection<ApiKey> ApiKeys { get; private set; } = [];
    public ICollection<Project> Projects { get; private set; } = [];
    public ICollection<EventRecord> Events { get; private set; } = [];
}
