using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
            options.IncludeScopes = false;
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
}
