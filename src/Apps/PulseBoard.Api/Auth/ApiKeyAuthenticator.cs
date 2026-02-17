using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

using PulseBoard.Infrastructure;
using PulseBoard.Infrastructure.Entities;

namespace PulseBoard.Api.Auth;

public static class ApiKeyAuthenticator
{
    public static async Task<Guid?> TryResolveTenantIdAsync(
        AppDbContext db,
        string rawApiKey,
        CancellationToken ct)
    {
        var hash = Sha256Hex(rawApiKey);

        ApiKey? apiKey = await db.ApiKeys
            .TagWith("ApiKeyAuthenticator.TryResolveTenantId")
            .Where(k => k.IsActive && k.KeyHash == hash)
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (apiKey is null) return null;

        apiKey.LastUsedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return apiKey.TenantId;
    }

    private static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
