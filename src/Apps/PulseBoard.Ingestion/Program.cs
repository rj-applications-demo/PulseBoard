using Azure.Messaging.ServiceBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OpenTelemetry;
using OpenTelemetry.Metrics;

using PulseBoard.Configuration;
using PulseBoard.Infrastructure;
using PulseBoard.Ingestion;

var builder = Host.CreateApplicationBuilder(args);

builder.AddPulseBoardCore("PulseBoard.Ingestion");

// OpenTelemetry
var otel = builder.AddPulseBoardTelemetry("PulseBoard.Ingestion");
otel.WithMetrics(metrics => metrics
    .AddPrometheusHttpListener(options =>
    {
        options.UriPrefixes = ["http://*:9464/"];
    }));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Sql")!;
    options.UseSqlServer(cs);
});

// Service Bus client
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(opts.ConnectionString);
});

// Service Bus sender for aggregate updates topic
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateSender(opts.AggregateUpdatesTopicName);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
