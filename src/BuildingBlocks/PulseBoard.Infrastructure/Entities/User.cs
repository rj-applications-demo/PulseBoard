namespace PulseBoard.Infrastructure.Entities;

public sealed class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public required Guid TenantId { get; init; }
    public required string Email { get; init; }

    public DateTimeOffset CreatedUtc { get; private set; } = DateTimeOffset.UtcNow;

    public Tenant Tenant { get; private set; } = null!;
    public ICollection<ApiKey> ApiKeys { get; private set; } = [];
}
