using PulseBoard.Infrastructure;

namespace PulseBoard.Api.Auth;

public sealed partial class ApiKeyMiddleware
{
    private static readonly PathString[] AnonymousPaths =
    [
        new("/"),
        new("/metrics"),
        new("/openapi")
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    [LoggerMessage(LogLevel.Warning, "API key authentication failed for {Path}")]
    private partial void LogAuthFailed(string path);

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (IsAnonymousPath(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader) ||
            string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            LogAuthFailed(context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new { error = "Missing X-Api-Key header." },
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        var identity = await ApiKeyAuthenticator.TryResolveAsync(
            db, apiKeyHeader!, context.RequestAborted).ConfigureAwait(false);

        if (identity is null)
        {
            LogAuthFailed(context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new { error = "Invalid API key." },
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        context.Items["TenantId"] = identity.TenantId;
        context.Items["ApiKeyTier"] = identity.Tier;

        await _next(context).ConfigureAwait(false);
    }

    private static bool IsAnonymousPath(PathString path)
    {
        foreach (var anonymous in AnonymousPaths)
        {
            if (path.Equals(anonymous, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments(anonymous, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
