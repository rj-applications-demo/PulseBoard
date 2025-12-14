using Microsoft.Extensions.Options;

using PulseBoard.Configuration;

namespace PulseBoard.Ingestion;

public partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ServiceBusOptions _serviceBusOptions;
    private readonly RedisOptions _redisOptions;

    public Worker(
        ILogger<Worker> logger,
        IOptions<ServiceBusOptions> serviceBusOptions,
        IOptions<RedisOptions> redisOptions)
    {
        _logger = logger;
        _serviceBusOptions = serviceBusOptions.Value;
        _redisOptions = redisOptions.Value;
    }

    [LoggerMessage(LogLevel.Information, "Worker running at: {Time}")]
    static partial void LogWorkerRunning(ILogger<Worker> logger, DateTimeOffset time);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            LogWorkerRunning(_logger, DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
        }
    }
}
