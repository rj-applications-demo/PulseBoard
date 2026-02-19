using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

using PulseBoard.Api.Auth;
using PulseBoard.Api.Tests.Helpers;
using PulseBoard.Domain;
using PulseBoard.Infrastructure.Entities;

namespace PulseBoard.Api.Tests.Auth;

public sealed class ApiKeyMiddlewareTests
{
    private static DefaultHttpContext CreateHttpContext(string path, string? apiKey = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = new PathString(path);
        context.Response.Body = new MemoryStream();
        if (apiKey is not null)
            context.Request.Headers["X-Api-Key"] = apiKey;
        return context;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/metrics")]
    [InlineData("/metrics/something")]
    [InlineData("/openapi")]
    [InlineData("/openapi/v1")]
    public async Task InvokeAsync_AnonymousPath_CallsNext(string path)
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var logger = Substitute.For<ILogger<ApiKeyMiddleware>>();
        var middleware = new ApiKeyMiddleware(next, logger);
        var context = CreateHttpContext(path);
        using var db = InMemoryDbContextFactory.Create();

        await middleware.InvokeAsync(context, db);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_MissingApiKeyHeader_Returns401()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var logger = Substitute.For<ILogger<ApiKeyMiddleware>>();
        var middleware = new ApiKeyMiddleware(next, logger);
        var context = CreateHttpContext("/events");
        using var db = InMemoryDbContextFactory.Create();

        await middleware.InvokeAsync(context, db);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_EmptyApiKeyHeader_Returns401()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var logger = Substitute.For<ILogger<ApiKeyMiddleware>>();
        var middleware = new ApiKeyMiddleware(next, logger);
        var context = CreateHttpContext("/events", apiKey: "   ");
        using var db = InMemoryDbContextFactory.Create();

        await middleware.InvokeAsync(context, db);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_InvalidApiKey_Returns401()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var logger = Substitute.For<ILogger<ApiKeyMiddleware>>();
        var middleware = new ApiKeyMiddleware(next, logger);
        var context = CreateHttpContext("/events", apiKey: "bad-key");
        using var db = InMemoryDbContextFactory.Create();

        await middleware.InvokeAsync(context, db);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ValidApiKey_CallsNextAndSetsTenantId()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var logger = Substitute.For<ILogger<ApiKeyMiddleware>>();
        var middleware = new ApiKeyMiddleware(next, logger);
        using var db = InMemoryDbContextFactory.Create();
        var tenant = new Tenant { Name = "Test" };
        db.Tenants.Add(tenant);
        var keyHash = ApiKeyAuthenticator.Sha256Hex("valid-key");
        db.ApiKeys.Add(new ApiKey
        {
            TenantId = tenant.Id,
            Name = "Test Key",
            KeyHash = keyHash,
            Tier = ApiKeyTier.Premium
        });
        await db.SaveChangesAsync();
        var context = CreateHttpContext("/events", apiKey: "valid-key");

        await middleware.InvokeAsync(context, db);

        Assert.True(nextCalled);
        Assert.Equal(tenant.Id, context.Items["TenantId"]);
        Assert.Equal(ApiKeyTier.Premium, context.Items["ApiKeyTier"]);
    }
}
