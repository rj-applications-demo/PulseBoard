namespace PulseBoard.Domain.Events.Entities;

public sealed class Project(string tenantId, string name)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = tenantId;
    public string Name { get; init; } = name;
    public long TotalEventCount { get; private set; }

    public void RecordEvents(long batchSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        TotalEventCount += batchSize;
    }
}
