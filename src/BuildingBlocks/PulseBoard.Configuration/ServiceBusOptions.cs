namespace PulseBoard.Configuration;

public sealed class ServiceBusOptions
{
    public string? ConnectionString { get; init; }
    public string QueueName { get; init; } = "ingestion-events";
}
