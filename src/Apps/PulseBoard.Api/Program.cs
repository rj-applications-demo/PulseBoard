using Azure.Messaging.ServiceBus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using PulseBoard.Configuration;
using PulseBoard.Infrastructure;

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

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok("PulseBoard API running"));
app.MapControllers();

app.Run();
