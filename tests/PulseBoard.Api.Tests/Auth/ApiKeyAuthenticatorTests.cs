using PulseBoard.Api.Auth;
using PulseBoard.Api.Tests.Helpers;
using PulseBoard.Domain;
using PulseBoard.Infrastructure.Entities;

namespace PulseBoard.Api.Tests.Auth;

public sealed class ApiKeyAuthenticatorTests
{
    [Fact]
    public void Sha256Hex_ReturnsConsistentHash()
    {
        var hash1 = ApiKeyAuthenticator.Sha256Hex("test-key");
        var hash2 = ApiKeyAuthenticator.Sha256Hex("test-key");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Sha256Hex_DifferentInputs_ReturnDifferentHashes()
    {
        var hash1 = ApiKeyAuthenticator.Sha256Hex("key-1");
        var hash2 = ApiKeyAuthenticator.Sha256Hex("key-2");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Sha256Hex_ReturnsUppercaseHex()
    {
        var hash = ApiKeyAuthenticator.Sha256Hex("test");

        Assert.Matches("^[A-F0-9]+$", hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public async Task TryResolveAsync_ValidActiveKey_ReturnsIdentity()
    {
        using var db = InMemoryDbContextFactory.Create();
        var tenant = new Tenant { Name = "Test" };
        db.Tenants.Add(tenant);
        var keyHash = ApiKeyAuthenticator.Sha256Hex("my-api-key");
        db.ApiKeys.Add(new ApiKey
        {
            TenantId = tenant.Id,
            Name = "Test Key",
            KeyHash = keyHash,
            Tier = ApiKeyTier.Standard
        });
        await db.SaveChangesAsync();

        var result = await ApiKeyAuthenticator.TryResolveAsync(db, "my-api-key", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.TenantId);
        Assert.Equal(ApiKeyTier.Standard, result.Tier);
    }

    [Fact]
    public async Task TryResolveAsync_ValidActiveKey_UpdatesLastUsedUtc()
    {
        using var db = InMemoryDbContextFactory.Create();
        var tenant = new Tenant { Name = "Test" };
        db.Tenants.Add(tenant);
        var keyHash = ApiKeyAuthenticator.Sha256Hex("my-api-key");
        var apiKey = new ApiKey
        {
            TenantId = tenant.Id,
            Name = "Test Key",
            KeyHash = keyHash
        };
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync();

        Assert.Null(apiKey.LastUsedUtc);

        await ApiKeyAuthenticator.TryResolveAsync(db, "my-api-key", CancellationToken.None);

        Assert.NotNull(apiKey.LastUsedUtc);
    }

    [Fact]
    public async Task TryResolveAsync_InactiveKey_ReturnsNull()
    {
        using var db = InMemoryDbContextFactory.Create();
        var tenant = new Tenant { Name = "Test" };
        db.Tenants.Add(tenant);
        var keyHash = ApiKeyAuthenticator.Sha256Hex("inactive-key");
        db.ApiKeys.Add(new ApiKey
        {
            TenantId = tenant.Id,
            Name = "Inactive Key",
            KeyHash = keyHash,
            IsActive = false
        });
        await db.SaveChangesAsync();

        var result = await ApiKeyAuthenticator.TryResolveAsync(db, "inactive-key", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryResolveAsync_NonExistentKey_ReturnsNull()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await ApiKeyAuthenticator.TryResolveAsync(db, "does-not-exist", CancellationToken.None);

        Assert.Null(result);
    }
}
