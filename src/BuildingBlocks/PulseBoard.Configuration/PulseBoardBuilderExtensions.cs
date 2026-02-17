using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace PulseBoard.Configuration;

public static class PulseBoardBuilderExtensions
{
    /// <summary>
    /// Apply core PulseBoard configuration:
    /// - Logging defaults
    /// - SQL connection string validation
    /// - ServiceBus + Redis options binding
    /// </summary>
    public static IHostApplicationBuilder AddPulseBoardCore(
        this IHostApplicationBuilder builder,
        string applicationName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IConfigurationManager configuration = builder.Configuration;

        // -------- Logging --------
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        });

        builder.Logging.SetMinimumLevel(LogLevel.Information);

        // Tag logs with app name for multi-service scenarios
        builder.Services.AddLogging(logging =>
        {
            logging.AddFilter(null, LogLevel.Information);
        });

        // Optional: set application name (useful in some logs/telemetry)
        builder.Configuration["ApplicationName"] = applicationName;

        // -------- SQL Connection --------
        var sqlConnectionString = configuration.GetConnectionString("Sql");
        if (string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Sql is not configured. " +
                "Set it in appsettings/appsettings.{Environment}.json " +
                "or via the ConnectionStrings__Sql environment variable.");
        }

        // -------- Options Binding --------
        // ServiceBus
        builder.Services
            .AddOptions<ServiceBusOptions>()
            .Bind(configuration.GetSection("ServiceBus"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                "ServiceBus:ConnectionString is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.QueueName),
                "ServiceBus:QueueName is required.")
            .ValidateOnStart();

        // Redis
        builder.Services
            .AddOptions<RedisOptions>()
            .Configure(o => o.ConnectionString = configuration.GetConnectionString("Redis"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                "ConnectionStrings:Redis is required.")
            .ValidateOnStart();

        return builder;
    }

    /// <summary>
    /// Configure shared OpenTelemetry telemetry: resource identity,
    /// common tracing/metrics instrumentation, and OTLP trace export to Tempo.
    /// Returns <see cref="IOpenTelemetryBuilder"/> for app-specific chaining.
    /// </summary>
    public static IOpenTelemetryBuilder AddPulseBoardTelemetry(
        this IHostApplicationBuilder builder,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var otlpEndpoint = builder.Configuration["Otlp:Endpoint"] ?? "http://tempo:4317";

        var otel = builder.Services.AddOpenTelemetry();

        otel.ConfigureResource(resource => resource
            .AddService(
                serviceName: serviceName,
                serviceNamespace: "PulseBoard",
                serviceVersion: typeof(PulseBoardBuilderExtensions).Assembly
                    .GetName().Version?.ToString() ?? "0.0.0"));

        otel.WithTracing(tracing => tracing
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.RecordException = true;
            })
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            }));

        otel.WithMetrics(metrics => metrics
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddMeter("System.Net.Http"));

        return otel;
    }
}
