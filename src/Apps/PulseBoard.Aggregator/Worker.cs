using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using PulseBoard.Aggregator.Services;
using PulseBoard.Configuration;
using PulseBoard.Contracts.Messaging;
using PulseBoard.Infrastructure;

namespace PulseBoard.Aggregator;

public sealed partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRedisTimeSeriesService _redisService;

    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _cachedUpdatesSender;
    private readonly ServiceBusOptions _serviceBusOptions;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IRedisTimeSeriesService redisService,
        ServiceBusClient serviceBusClient,
        ServiceBusSender cachedUpdatesSender,
        IOptions<ServiceBusOptions> serviceBusOptions)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _redisService = redisService;
        _serviceBusClient = serviceBusClient;
        _cachedUpdatesSender = cachedUpdatesSender;
        _serviceBusOptions = serviceBusOptions.Value;
    }

    [LoggerMessage(LogLevel.Information, "Aggregator worker started. Topic={TopicName}, Subscription={SubscriptionName}")]
    private static partial void LogStarted(ILogger<Worker> logger, string topicName, string subscriptionName);

    [LoggerMessage(LogLevel.Debug, "Processed aggregate update. TenantId={TenantId}, ProjectKey={ProjectKey}, Metric={Metric}, Dimension={DimensionKey}")]
    private static partial void LogProcessed(ILogger<Worker> logger, Guid tenantId, string projectKey, string metric, string? dimensionKey);

    [LoggerMessage(LogLevel.Error, "Failed processing aggregate message. MessageId={MessageId}")]
    private static partial void LogProcessingFailed(ILogger<Worker> logger, string messageId, Exception ex);

    [LoggerMessage(LogLevel.Error, "Service Bus processor error. Entity={EntityPath}")]
    private static partial void LogServiceBusError(ILogger<Worker> logger, string entityPath, Exception exception);

    [LoggerMessage(LogLevel.Debug, "SQL refresh completed for long intervals.")]
    private static partial void LogSqlRefreshCompleted(ILogger<Worker> logger);

    [LoggerMessage(LogLevel.Error, "SQL refresh failed.")]
    private static partial void LogSqlRefreshFailed(ILogger<Worker> logger, Exception ex);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger, _serviceBusOptions.AggregateUpdatesTopicName, _serviceBusOptions.AggregateUpdatesSubscription);

        var processor = _serviceBusClient.CreateProcessor(
            _serviceBusOptions.AggregateUpdatesTopicName,
            _serviceBusOptions.AggregateUpdatesSubscription,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 4,
                PrefetchCount = 50
            });

        processor.ProcessMessageAsync += args => HandleMessageAsync(args, stoppingToken);
        processor.ProcessErrorAsync += args =>
        {
            LogServiceBusError(_logger, args.EntityPath, args.Exception);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken).ConfigureAwait(false);

        // Start periodic SQL refresh for long intervals
        _ = RunPeriodicSqlRefreshAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        await processor.StopProcessingAsync(CancellationToken.None).ConfigureAwait(false);
        await processor.DisposeAsync().ConfigureAwait(false);
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args, CancellationToken ct)
    {
        if (!TryDeserialize(args.Message.Body, out var msg))
        {
            await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed", "Invalid JSON.", ct).ConfigureAwait(false);
            return;
        }

        try
        {
            // Increment Redis second counter
            await _redisService.IncrementSecondCounterAsync(
                msg.TenantId,
                msg.ProjectId,
                msg.Metric,
                msg.DimensionKey,
                msg.EventTimestampUtc,
                ct).ConfigureAwait(false);

            // Rebuild 60-second sorted set
            await _redisService.RebuildSixtySecondSeriesAsync(
                msg.TenantId,
                msg.ProjectId,
                msg.Metric,
                msg.DimensionKey,
                ct).ConfigureAwait(false);

            // Get updated time series for 60s interval
            var sixtySecondSeries = await _redisService.GetTimeSeriesAsync(
                msg.TenantId,
                msg.ProjectId,
                msg.Metric,
                msg.DimensionKey,
                "60s",
                ct).ConfigureAwait(false);

            // Publish cached aggregate update
            await PublishCachedAggregateUpdateAsync(
                msg.TenantId,
                msg.ProjectId,
                msg.ProjectKey,
                msg.Metric,
                msg.DimensionKey,
                sixtySecondSeries,
                ct).ConfigureAwait(false);

            LogProcessed(_logger, msg.TenantId, msg.ProjectKey, msg.Metric, msg.DimensionKey);
        }
        catch (Exception ex)
        {
            LogProcessingFailed(_logger, args.Message.MessageId, ex);
            throw;
        }

        await args.CompleteMessageAsync(args.Message, ct).ConfigureAwait(false);
    }

    private async Task RunPeriodicSqlRefreshAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct).ConfigureAwait(false);
                await RefreshLongIntervalsFromSqlAsync(ct).ConfigureAwait(false);
                LogSqlRefreshCompleted(_logger);
            }
            catch (OperationCanceledException)
            {
                break;
            }
#pragma warning disable CA1031 // Background task should not crash on transient errors
            catch (Exception ex)
            {
                LogSqlRefreshFailed(_logger, ex);
            }
#pragma warning restore CA1031
        }
    }

    private async Task RefreshLongIntervalsFromSqlAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var sixtyMinutesAgo = now.AddMinutes(-60);
        var twentyFourHoursAgo = now.AddHours(-24);

        // Get distinct tenant/project/metric/dimension combinations with recent activity
        var activeKeys = await db.AggregateBuckets
            .TagWith("Aggregator.Worker.RefreshLongIntervals_ActiveKeys")
            .Where(b => b.BucketStartUtc >= twentyFourHoursAgo)
            .Select(b => new { b.TenantId, b.ProjectId, b.Metric, b.DimensionKey })
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var key in activeKeys)
        {
            // Refresh 60m series
            var sixtyMinuteBuckets = await db.AggregateBuckets
                .TagWith("Aggregator.Worker.RefreshLongIntervals_60mBuckets")
                .Where(b => b.TenantId == key.TenantId
                    && b.ProjectId == key.ProjectId
                    && b.Metric == key.Metric
                    && b.DimensionKey == key.DimensionKey
                    && b.Interval == "1m"
                    && b.BucketStartUtc >= sixtyMinutesAgo)
                .Select(b => new TimeSeriesDataPoint { Timestamp = b.BucketStartUtc, Value = b.Value })
                .ToListAsync(ct).ConfigureAwait(false);

            if (sixtyMinuteBuckets.Count > 0)
            {
                await _redisService.UpdateLongIntervalSeriesAsync(
                    key.TenantId,
                    key.ProjectId,
                    key.Metric,
                    key.DimensionKey,
                    "60m",
                    sixtyMinuteBuckets,
                    ct).ConfigureAwait(false);
            }

            // Refresh 24h series (aggregated by hour)
            var twentyFourHourBuckets = await db.AggregateBuckets
                .TagWith("Aggregator.Worker.RefreshLongIntervals_24hBuckets")
                .Where(b => b.TenantId == key.TenantId
                    && b.ProjectId == key.ProjectId
                    && b.Metric == key.Metric
                    && b.DimensionKey == key.DimensionKey
                    && b.Interval == "1m"
                    && b.BucketStartUtc >= twentyFourHoursAgo)
                .GroupBy(b => new { b.BucketStartUtc.Year, b.BucketStartUtc.Month, b.BucketStartUtc.Day, b.BucketStartUtc.Hour })
                .Select(g => new TimeSeriesDataPoint
                {
                    Timestamp = new DateTimeOffset(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0, TimeSpan.Zero),
                    Value = g.Sum(b => b.Value)
                })
                .ToListAsync(ct).ConfigureAwait(false);

            if (twentyFourHourBuckets.Count > 0)
            {
                await _redisService.UpdateLongIntervalSeriesAsync(
                    key.TenantId,
                    key.ProjectId,
                    key.Metric,
                    key.DimensionKey,
                    "24h",
                    twentyFourHourBuckets,
                    ct).ConfigureAwait(false);
            }
        }
    }

    private async Task PublishCachedAggregateUpdateAsync(
        Guid tenantId,
        Guid projectId,
        string projectKey,
        string metric,
        string? dimensionKey,
        IReadOnlyList<TimeSeriesDataPoint> sixtySecondSeries,
        CancellationToken ct)
    {
        var msg = new CachedAggregateUpdatedMessage
        {
            TenantId = tenantId,
            ProjectId = projectId,
            ProjectKey = projectKey,
            Metric = metric,
            DimensionKey = dimensionKey,
            UpdatedIntervals = new[] { "60s" },
            TimeSeries = new Dictionary<string, IReadOnlyList<TimeSeriesDataPoint>>
            {
                ["60s"] = sixtySecondSeries
            }
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(msg);
        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json"
        };

        await _cachedUpdatesSender.SendMessageAsync(sbMessage, ct).ConfigureAwait(false);
    }

    private static bool TryDeserialize(BinaryData body, [NotNullWhen(true)] out AggregateUpdatedMessage? msg)
    {
        try
        {
            msg = JsonSerializer.Deserialize<AggregateUpdatedMessage>(body);
            return msg is not null;
        }
        catch (JsonException)
        {
            msg = null;
            return false;
        }
    }
}
