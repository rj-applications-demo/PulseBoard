using System.Globalization;
using System.Threading.RateLimiting;

using Azure.Messaging.ServiceBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using PulseBoard.Api.Auth;
using PulseBoard.Api.Seeding;
using PulseBoard.Api.Services;
using PulseBoard.Configuration;
using PulseBoard.Domain;
using PulseBoard.Infrastructure;

using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Shared core config (logging + options + validation)
builder.AddPulseBoardCore("PulseBoard.Api");

// OpenTelemetry
var otel = builder.AddPulseBoardTelemetry("PulseBoard.Api");
otel.WithTracing(tracing => tracing
    .AddAspNetCoreInstrumentation()
    .AddRedisInstrumentation());
otel.WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddPrometheusExporter());

builder.Services.AddControllers();

// JSON + HTTP defaults
builder.Services.AddPulseBoardApiDefaults();

// DbContext, using validated connection string
var sqlConnectionString = builder.Configuration.GetConnectionString("Sql")!;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(opts.ConnectionString);
});

builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateSender(opts.QueueName);
});

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(opts.ConnectionString!);
});

// Metrics service
builder.Services.AddScoped<IMetricsService, MetricsService>();

// Seeding
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection("Seed"));
builder.Services.AddScoped<DatabaseSeeder>();

// Rate limiting
var rateLimitConfig = builder.Configuration
    .GetSection("RateLimiting")
    .Get<RateLimitOptions>() ?? new RateLimitOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (!context.Items.TryGetValue("ApiKeyTier", out var tierObj) || tierObj is not ApiKeyTier tier)
            return RateLimitPartition.GetNoLimiter("anonymous");

        var tenantId = context.Items.TryGetValue("TenantId", out var tenantObj)
            ? tenantObj?.ToString() ?? "anonymous"
            : "anonymous";

        var tierConfig = tier switch
        {
            ApiKeyTier.Standard => rateLimitConfig.Standard,
            ApiKeyTier.Premium => rateLimitConfig.Premium,
            _ => rateLimitConfig.Free
        };

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{tenantId}:{tier}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = tierConfig.PermitLimit,
                Window = TimeSpan.FromSeconds(tierConfig.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/json";

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
            ? retryAfterValue
            : TimeSpan.FromSeconds(60);

        context.HttpContext.Response.Headers.RetryAfter =
            ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Rate limit exceeded. Try again later.", retryAfterSeconds = (int)retryAfter.TotalSeconds },
            ct).ConfigureAwait(false);
    };
});

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Run database seeding
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync().ConfigureAwait(false);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok("PulseBoard API running"));

// Global auth middleware → rate limiter → controllers
app.UseMiddleware<ApiKeyMiddleware>();
app.UseRateLimiter();

app.MapControllers();
app.MapPrometheusScrapingEndpoint();

await app.RunAsync().ConfigureAwait(false);
