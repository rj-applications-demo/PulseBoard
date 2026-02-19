namespace PulseBoard.Configuration;

public sealed class RateLimitOptions
{
    public TierRateLimitOptions Free { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public TierRateLimitOptions Standard { get; init; } = new() { PermitLimit = 60, WindowSeconds = 60 };
    public TierRateLimitOptions Premium { get; init; } = new() { PermitLimit = 600, WindowSeconds = 60 };
}

public sealed class TierRateLimitOptions
{
    public int PermitLimit { get; init; }
    public int WindowSeconds { get; init; }
}
