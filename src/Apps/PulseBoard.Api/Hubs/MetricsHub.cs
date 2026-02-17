using Microsoft.AspNetCore.SignalR;

using PulseBoard.Api.Auth;
using PulseBoard.Infrastructure;

namespace PulseBoard.Api.Hubs;

public sealed class MetricsHub : Hub
{
    private readonly AppDbContext _db;

    public MetricsHub(AppDbContext db)
    {
        _db = db;
    }

    public async Task Subscribe(SubscribeRequest request)
    {
        var tenantId = await GetTenantIdAsync().ConfigureAwait(false);
        if (tenantId is null)
        {
            await Clients.Caller.SendAsync("Error", "Invalid API key.").ConfigureAwait(false);
            return;
        }

        string groupName = BuildGroupName(tenantId.Value, request.ProjectKey, request.Metric, request.Dimension, request.Interval);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
        await Clients.Caller.SendAsync("Subscribed", new { groupName, request.ProjectKey, request.Metric, request.Dimension, request.Interval }).ConfigureAwait(false);
    }

    public async Task Unsubscribe(UnsubscribeRequest request)
    {
        var tenantId = await GetTenantIdAsync().ConfigureAwait(false);
        if (tenantId is null)
            return;

        string groupName = BuildGroupName(tenantId.Value, request.ProjectKey, request.Metric, request.Dimension, request.Interval);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
        await Clients.Caller.SendAsync("Unsubscribed", new { groupName }).ConfigureAwait(false);
    }

    private async Task<Guid?> GetTenantIdAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext is null)
            return null;

        if (!httpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader) ||
            string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            // Try query string for WebSocket connections
            var apiKey = httpContext.Request.Query["apiKey"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            return await ApiKeyAuthenticator.TryResolveTenantIdAsync(_db, apiKey, CancellationToken.None).ConfigureAwait(false);
        }

        return await ApiKeyAuthenticator.TryResolveTenantIdAsync(_db, apiKeyHeader!, CancellationToken.None).ConfigureAwait(false);
    }

    internal static string BuildGroupName(Guid tenantId, string projectKey, string metric, string? dimension, string interval)
    {
        var dim = string.IsNullOrEmpty(dimension) ? "" : $":{dimension}";
        return $"{tenantId}:{projectKey}:{metric}{dim}:{interval}";
    }
}
