namespace PulseBoard.Infrastructure.Entities;

public sealed class ApiKey
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public required Guid TenantId { get; init; }
    public Guid? UserId { get; set; }

    public required string Name { get; init; }
    public required string KeyHash { get; init; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedUtc { get; set; }

    public Tenant Tenant { get; private set; } = null!;
    public User? User { get; private set; }
}
