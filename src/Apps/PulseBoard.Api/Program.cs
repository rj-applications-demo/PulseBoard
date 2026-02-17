using Azure.Messaging.ServiceBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using PulseBoard.Api.Hubs;
using PulseBoard.Api.Seeding;
using PulseBoard.Api.Services;
using PulseBoard.Configuration;
using PulseBoard.Infrastructure;

using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Shared core config (logging + options + validation)
builder.AddPulseBoardCore("PulseBoard.Api");

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

// SignalR
builder.Services.AddSignalR();
builder.Services.AddHostedService<SignalRPusherService>();

// Seeding
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection("Seed"));
builder.Services.AddScoped<DatabaseSeeder>();

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
app.MapControllers();
app.MapHub<MetricsHub>("/hubs/metrics");

await app.RunAsync().ConfigureAwait(false);
