using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using PulseBoard.Configuration;
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

    [LoggerMessage(LogLevel.Information, "Seeded API key for tenant: {TenantName}")]
    private partial void LogSeededApiKey(string tenantName);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await EnsureTenantAsync("Guest", _options.Guest.ApiKey, ct).ConfigureAwait(false);
        await EnsureTenantAsync("Dev", _options.Dev.ApiKey, ct).ConfigureAwait(false);
    }

    private async Task EnsureTenantAsync(string name, string? rawApiKey, CancellationToken ct)
    {
        Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Name == name, ct).ConfigureAwait(false);

        if (tenant is null)
        {
            tenant = new Tenant { Name = name };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            LogSeededTenant(name, tenant.Id);
        }

        if (string.IsNullOrWhiteSpace(rawApiKey))
            return;

        string keyHash = Sha256Hex(rawApiKey);
        bool keyExists = await _db.ApiKeys.AnyAsync(k => k.TenantId == tenant.Id && k.KeyHash == keyHash, ct).ConfigureAwait(false);

        if (!keyExists)
        {
            _db.ApiKeys.Add(new ApiKey
            {
                TenantId = tenant.Id,
                Name = $"{name} Seed Key",
                KeyHash = keyHash
            });
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            LogSeededApiKey(name);
        }
    }

    private static string Sha256Hex(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
