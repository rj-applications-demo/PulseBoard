using Azure.Messaging.ServiceBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using PulseBoard.Aggregator;
using PulseBoard.Aggregator.Services;
using PulseBoard.Configuration;
using PulseBoard.Infrastructure;

using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.AddPulseBoardCore("PulseBoard.Aggregator");

// OpenTelemetry
var otel = builder.AddPulseBoardTelemetry("PulseBoard.Aggregator");
otel.WithTracing(tracing => tracing
    .AddRedisInstrumentation());
otel.WithMetrics(metrics => metrics
    .AddPrometheusHttpListener(options =>
    {
        options.UriPrefixes = ["http://*:9465/"];
    }));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Sql")!;
    options.UseSqlServer(cs);
});

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(opts.ConnectionString!);
});

builder.Services.AddSingleton<IRedisTimeSeriesService, RedisTimeSeriesService>();

// Service Bus client
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(opts.ConnectionString);
});

// Service Bus sender for cached aggregate updates topic
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateSender(opts.CachedAggregateUpdatesTopicName);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
