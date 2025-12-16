namespace PulseBoard.Configuration;

public sealed class SeedOptions
{
    public TenantSeedOptions Guest { get; init; } = new();
    public TenantSeedOptions Dev { get; init; } = new();
}

public sealed class TenantSeedOptions
{
    public string? ApiKey { get; init; }
}
