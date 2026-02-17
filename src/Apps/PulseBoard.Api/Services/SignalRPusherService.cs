using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

using PulseBoard.Api.Hubs;
using PulseBoard.Configuration;
using PulseBoard.Contracts.Messaging;

namespace PulseBoard.Api.Services;

public sealed partial class SignalRPusherService : BackgroundService
{
    private readonly ILogger<SignalRPusherService> _logger;
    private readonly IHubContext<MetricsHub> _hubContext;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusOptions _serviceBusOptions;

    public SignalRPusherService(
        ILogger<SignalRPusherService> logger,
        IHubContext<MetricsHub> hubContext,
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusOptions> serviceBusOptions)
    {
        _logger = logger;
        _hubContext = hubContext;
        _serviceBusClient = serviceBusClient;
        _serviceBusOptions = serviceBusOptions.Value;
    }

    [LoggerMessage(LogLevel.Information, "SignalR pusher started. Topic={TopicName}, Subscription={SubscriptionName}")]
    private static partial void LogStarted(ILogger<SignalRPusherService> logger, string topicName, string subscriptionName);

    [LoggerMessage(LogLevel.Debug, "Pushed update to group. Group={GroupName}")]
    private static partial void LogPushed(ILogger<SignalRPusherService> logger, string groupName);

    [LoggerMessage(LogLevel.Error, "Failed processing cached aggregate message. MessageId={MessageId}")]
    private static partial void LogProcessingFailed(ILogger<SignalRPusherService> logger, string messageId, Exception ex);

    [LoggerMessage(LogLevel.Error, "Service Bus processor error. Entity={EntityPath}")]
    private static partial void LogServiceBusError(ILogger<SignalRPusherService> logger, string entityPath, Exception exception);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger, _serviceBusOptions.CachedAggregateUpdatesTopicName, _serviceBusOptions.CachedAggregateUpdatesSubscription);

        var processor = _serviceBusClient.CreateProcessor(
            _serviceBusOptions.CachedAggregateUpdatesTopicName,
            _serviceBusOptions.CachedAggregateUpdatesSubscription,
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
            // Push updates to SignalR groups for each updated interval
            foreach (var interval in msg.UpdatedIntervals)
            {
                if (!msg.TimeSeries.TryGetValue(interval, out var dataPoints))
                    continue;

                string groupName = MetricsHub.BuildGroupName(msg.TenantId, msg.ProjectKey, msg.Metric, msg.DimensionKey, interval);

                var update = new TimeSeriesUpdateMessage
                {
                    ProjectKey = msg.ProjectKey,
                    Metric = msg.Metric,
                    Interval = interval,
                    Dimension = msg.DimensionKey,
                    DataPoints = dataPoints.Select(dp => new Hubs.TimeSeriesPoint
                    {
                        Timestamp = dp.Timestamp,
                        Value = dp.Value
                    }).ToList()
                };

                await _hubContext.Clients.Group(groupName).SendAsync("TimeSeriesUpdate", update, ct).ConfigureAwait(false);
                LogPushed(_logger, groupName);
            }
        }
        catch (Exception ex)
        {
            LogProcessingFailed(_logger, args.Message.MessageId, ex);
            throw;
        }

        await args.CompleteMessageAsync(args.Message, ct).ConfigureAwait(false);
    }

    private static bool TryDeserialize(BinaryData body, [NotNullWhen(true)] out CachedAggregateUpdatedMessage? msg)
    {
        try
        {
            msg = JsonSerializer.Deserialize<CachedAggregateUpdatedMessage>(body);
            return msg is not null;
        }
        catch (JsonException)
        {
            msg = null;
            return false;
        }
    }
}
