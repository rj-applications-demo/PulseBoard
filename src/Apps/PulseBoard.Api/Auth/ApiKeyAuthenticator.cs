using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

using PulseBoard.Domain;
using PulseBoard.Infrastructure;
using PulseBoard.Infrastructure.Entities;

namespace PulseBoard.Api.Auth;

public sealed record ApiKeyIdentity(Guid TenantId, ApiKeyTier Tier);

public static class ApiKeyAuthenticator
{
    public static async Task<ApiKeyIdentity?> TryResolveAsync(
        AppDbContext db,
        string rawApiKey,
        CancellationToken ct)
    {
        var hash = Sha256Hex(rawApiKey);

        ApiKey? apiKey = await db.ApiKeys
            .TagWith("ApiKeyAuthenticator.TryResolve")
            .Where(k => k.IsActive && k.KeyHash == hash)
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (apiKey is null) return null;

        apiKey.LastUsedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new ApiKeyIdentity(apiKey.TenantId, apiKey.Tier);
    }

    internal static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
