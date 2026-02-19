using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using PulseBoard.Configuration;
using PulseBoard.Domain;
using PulseBoard.Infrastructure;
using PulseBoard.Infrastructure.Entities;

namespace PulseBoard.Api.Seeding;

public sealed partial class DatabaseSeeder
{
    private readonly AppDbContext _db;
    private readonly SeedOptions _options;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext db,
        IOptions<SeedOptions> options,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    [LoggerMessage(LogLevel.Information, "Seeded tenant: {TenantName} ({TenantId})")]
    private partial void LogSeededTenant(string tenantName, Guid tenantId);

    [LoggerMessage(LogLevel.Information, "Seeded API key for tenant: {TenantName} (Tier={Tier})")]
    private partial void LogSeededApiKey(string tenantName, ApiKeyTier tier);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await EnsureTenantAsync("Guest", _options.Guest, ct).ConfigureAwait(false);
        await EnsureTenantAsync("Dev", _options.Dev, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(_options.Pro.ApiKey))
        {
            await EnsureTenantAsync("Pro", _options.Pro, ct).ConfigureAwait(false);
        }
    }

    private async Task EnsureTenantAsync(string name, TenantSeedOptions seedOptions, CancellationToken ct)
    {
        Tenant? tenant = await _db.Tenants.TagWith("DatabaseSeeder.EnsureTenant").FirstOrDefaultAsync(t => t.Name == name, ct).ConfigureAwait(false);

        if (tenant is null)
        {
            tenant = new Tenant { Name = name };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            LogSeededTenant(name, tenant.Id);
        }

        if (string.IsNullOrWhiteSpace(seedOptions.ApiKey))
            return;

        string keyHash = Sha256Hex(seedOptions.ApiKey);

        ApiKey? existingKey = await _db.ApiKeys
            .TagWith("DatabaseSeeder.EnsureTenant_CheckApiKeyExists")
            .FirstOrDefaultAsync(k => k.TenantId == tenant.Id && k.KeyHash == keyHash, ct)
            .ConfigureAwait(false);

        if (existingKey is null)
        {
            _db.ApiKeys.Add(new ApiKey
            {
                TenantId = tenant.Id,
                Name = $"{name} Seed Key",
                KeyHash = keyHash,
                Tier = seedOptions.Tier
            });
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            LogSeededApiKey(name, seedOptions.Tier);
        }
        else if (existingKey.Tier != seedOptions.Tier)
        {
            existingKey.Tier = seedOptions.Tier;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            LogSeededApiKey(name, seedOptions.Tier);
        }
    }

    private static string Sha256Hex(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
