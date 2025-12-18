using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using PulseBoard.Configuration;
using PulseBoard.Contracts.Messaging;
using PulseBoard.Infrastructure;
using PulseBoard.Infrastructure.Entities;

namespace PulseBoard.Ingestion;

public sealed partial class Worker : BackgroundService
{
    private const string DefaultMetric = "event.count";
    private const string OneMinuteInterval = "1m";

    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusOptions _serviceBusOptions;
    private readonly ServiceBusSender _aggregateSender;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        ServiceBusClient serviceBusClient,
        ServiceBusSender aggregateSender,
        IOptions<ServiceBusOptions> serviceBusOptions)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _serviceBusClient = serviceBusClient;
        _aggregateSender = aggregateSender;
        _serviceBusOptions = serviceBusOptions.Value;
    }

    [LoggerMessage(LogLevel.Information, "Ingestion worker started. Queue={QueueName}")]
    private static partial void LogStarted(ILogger<Worker> logger, string queueName);

    [LoggerMessage(LogLevel.Warning, "Duplicate event ignored. EventId={EventId}")]
    private static partial void LogDuplicate(ILogger<Worker> logger, string eventId);

    [LoggerMessage(LogLevel.Error, "Failed processing message. MessageId={MessageId}")]
    private static partial void LogProcessingFailed(ILogger<Worker> logger, string messageId, Exception ex);

    [LoggerMessage(LogLevel.Error, "Service Bus processor error. Entity={EntityPath}")]
    private static partial void LogServiceBusError(ILogger<Worker> logger, string entityPath, Exception exception);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger, _serviceBusOptions.QueueName);

        var processor = _serviceBusClient.CreateProcessor(_serviceBusOptions.QueueName, new ServiceBusProcessorOptions
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

        // Keep the background service alive until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
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

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var projectKey = msg.ProjectKey.Trim();
        Guid projectId = await EnsureProjectAsync(db, msg.TenantId, projectKey, ct).ConfigureAwait(false);

        db.EventRecords.Add(new EventRecord
        {
            TenantId = msg.TenantId,
            EventId = msg.EventId,
            ProjectId = projectId,
            ProjectKey = projectKey,
            TimestampUtc = msg.Timestamp,
            PayloadJson = msg.Payload
        });

        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await UpdateProjectCountersAsync(db, projectId, msg.Timestamp, ct).ConfigureAwait(false);

            var bucketStart = TruncateToMinute(msg.Timestamp);

            // Upsert aggregate bucket (no dimension)
            await UpsertAggregateBucketAsync(db, msg.TenantId, projectId, bucketStart, dimensionKey: null, ct).ConfigureAwait(false);
            await PublishAggregateUpdatedAsync(msg.TenantId, projectId, projectKey, bucketStart, dimensionKey: null, msg.Timestamp, ct).ConfigureAwait(false);

            // If event has dimension, also upsert dimension-specific bucket
            if (!string.IsNullOrEmpty(msg.DimensionKey))
            {
                await UpsertAggregateBucketAsync(db, msg.TenantId, projectId, bucketStart, msg.DimensionKey, ct).ConfigureAwait(false);
                await PublishAggregateUpdatedAsync(msg.TenantId, projectId, projectKey, bucketStart, msg.DimensionKey, msg.Timestamp, ct).ConfigureAwait(false);
            }
        }
        catch (DbUpdateException ex) when (IsUniqueEventIdViolation(ex))
        {
            LogDuplicate(_logger, msg.EventId);
        }

        await args.CompleteMessageAsync(args.Message, ct).ConfigureAwait(false);
    }

    private static bool TryDeserialize(BinaryData body, [NotNullWhen(true)] out IncomingEventMessage? msg)
    {
        try
        {
            msg = JsonSerializer.Deserialize<IncomingEventMessage>(body);
            return msg is not null;
        }
        catch (JsonException)
        {
            msg = null;
            return false;
        }
    }

    private static async Task<Guid> EnsureProjectAsync(AppDbContext db, Guid tenantId, string projectKey, CancellationToken ct)
    {
        Guid? projectId = await db.Projects
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Key == projectKey)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (projectId.HasValue)
            return projectId.Value;

        var project = new Project { TenantId = tenantId, Key = projectKey, Name = projectKey };
        db.Projects.Add(project);

        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return project.Id;
        }
        catch (DbUpdateException)
        {
            db.Entry(project).State = EntityState.Detached;
            return await db.Projects
                .Where(p => p.TenantId == tenantId && p.Key == projectKey)
                .Select(p => p.Id)
                .FirstAsync(ct).ConfigureAwait(false);
        }
    }

    private static async Task UpdateProjectCountersAsync(AppDbContext db, Guid projectId, DateTimeOffset timestampUtc, CancellationToken ct)
    {
        await db.Projects
            .Where(p => p.Id == projectId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.TotalEventCount, p => p.TotalEventCount + 1)
                .SetProperty(p => p.LastEventUtc, p => p.LastEventUtc == null || timestampUtc > p.LastEventUtc ? timestampUtc : p.LastEventUtc),
            ct).ConfigureAwait(false);
    }
    private static bool IsUniqueEventIdViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("IX_EventRecords_TenantId_EventId", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset TruncateToMinute(DateTimeOffset timestamp)
    {
        return new DateTimeOffset(
            timestamp.Year,
            timestamp.Month,
            timestamp.Day,
            timestamp.Hour,
            timestamp.Minute,
            second: 0,
            timestamp.Offset);
    }

    private static async Task UpsertAggregateBucketAsync(
        AppDbContext db,
        Guid tenantId,
        Guid projectId,
        DateTimeOffset bucketStart,
        string? dimensionKey,
        CancellationToken ct)
    {
        // Try atomic update first
        int updated = await db.AggregateBuckets
            .Where(b => b.TenantId == tenantId
                && b.ProjectId == projectId
                && b.Metric == DefaultMetric
                && b.Interval == OneMinuteInterval
                && b.BucketStartUtc == bucketStart
                && b.DimensionKey == dimensionKey)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Value, b => b.Value + 1)
                .SetProperty(b => b.UpdatedUtc, DateTimeOffset.UtcNow),
            ct).ConfigureAwait(false);

        if (updated > 0)
            return;

        // Insert new bucket if update didn't find one
        try
        {
            db.AggregateBuckets.Add(new AggregateBucket
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Metric = DefaultMetric,
                Interval = OneMinuteInterval,
                BucketStartUtc = bucketStart,
                DimensionKey = dimensionKey,
                Value = 1,
                UpdatedUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // Race condition: another process inserted - retry update
            db.ChangeTracker.Clear();
            await db.AggregateBuckets
                .Where(b => b.TenantId == tenantId
                    && b.ProjectId == projectId
                    && b.Metric == DefaultMetric
                    && b.Interval == OneMinuteInterval
                    && b.BucketStartUtc == bucketStart
                    && b.DimensionKey == dimensionKey)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Value, b => b.Value + 1)
                    .SetProperty(b => b.UpdatedUtc, DateTimeOffset.UtcNow),
                ct).ConfigureAwait(false);
        }
    }

    private async Task PublishAggregateUpdatedAsync(
        Guid tenantId,
        Guid projectId,
        string projectKey,
        DateTimeOffset bucketStart,
        string? dimensionKey,
        DateTimeOffset eventTimestamp,
        CancellationToken ct)
    {
        var msg = new AggregateUpdatedMessage
        {
            TenantId = tenantId,
            ProjectId = projectId,
            ProjectKey = projectKey,
            Metric = DefaultMetric,
            Interval = OneMinuteInterval,
            BucketStartUtc = bucketStart,
            DimensionKey = dimensionKey,
            EventTimestampUtc = eventTimestamp
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(msg);
        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json"
        };

        await _aggregateSender.SendMessageAsync(sbMessage, ct).ConfigureAwait(false);
    }
}
