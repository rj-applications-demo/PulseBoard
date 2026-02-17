using System.Globalization;

using Microsoft.EntityFrameworkCore;

using PulseBoard.Api.Models.Response;
using PulseBoard.Infrastructure;

using StackExchange.Redis;

namespace PulseBoard.Api.Services;

public sealed class MetricsService : IMetricsService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly AppDbContext _db;

    public MetricsService(IConnectionMultiplexer redis, AppDbContext db)
    {
        _redis = redis;
        _db = db;
    }

    public async Task<TimeSeriesResponse?> GetTimeSeriesAsync(
        Guid tenantId,
        string projectKey,
        string metric,
        string interval,
        string? dimensionKey,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken ct)
    {
        // Lookup project
        var project = await _db.Projects
            .AsNoTracking()
            .TagWith("MetricsService.GetTimeSeries_FindProject")
            .Where(p => p.TenantId == tenantId && p.Key == projectKey)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (project is null)
            return null;

        // Try Redis first
        var redisResult = await TryGetFromRedisAsync(tenantId, project.Id, metric, interval, dimensionKey, fromUtc, toUtc).ConfigureAwait(false);
        if (redisResult is not null)
        {
            return new TimeSeriesResponse
            {
                ProjectKey = projectKey,
                Metric = metric,
                Interval = interval,
                DimensionKey = dimensionKey,
                Source = "redis",
                DataPoints = redisResult
            };
        }

        // Fallback to SQL
        var sqlResult = await GetFromSqlAsync(tenantId, project.Id, metric, interval, dimensionKey, fromUtc, toUtc, ct).ConfigureAwait(false);
        return new TimeSeriesResponse
        {
            ProjectKey = projectKey,
            Metric = metric,
            Interval = interval,
            DimensionKey = dimensionKey,
            Source = "sql",
            DataPoints = sqlResult
        };
    }

    public async Task<TopProjectsResponse> GetTopProjectsAsync(
        Guid tenantId,
        string metric,
        string interval,
        string? dimensionKey,
        int limit,
        CancellationToken ct)
    {
        // Get time range based on interval
        var (from, to) = GetTimeRangeForInterval(interval);

        var topProjects = await _db.AggregateBuckets
            .AsNoTracking()
            .TagWith("MetricsService.GetTopProjects")
            .Where(b => b.TenantId == tenantId
                && b.Metric == metric
                && b.Interval == "1m"
                && b.DimensionKey == dimensionKey
                && b.BucketStartUtc >= from
                && b.BucketStartUtc <= to)
            .GroupBy(b => b.ProjectId)
            .Select(g => new { ProjectId = g.Key, Total = g.Sum(b => b.Value) })
            .OrderByDescending(x => x.Total)
            .Take(limit)
            .ToListAsync(ct).ConfigureAwait(false);

        var projectIds = topProjects.Select(p => p.ProjectId).ToList();
        var projectKeys = await _db.Projects
            .AsNoTracking()
            .TagWith("MetricsService.GetTopProjects_ProjectKeys")
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Key, ct).ConfigureAwait(false);

        var projects = topProjects
            .Where(p => projectKeys.ContainsKey(p.ProjectId))
            .Select(p => new ProjectMetric
            {
                ProjectKey = projectKeys[p.ProjectId],
                Value = p.Total
            })
            .ToList();

        return new TopProjectsResponse
        {
            Metric = metric,
            Interval = interval,
            DimensionKey = dimensionKey,
            Source = "sql",
            Projects = projects
        };
    }

    private async Task<List<TimeSeriesPoint>?> TryGetFromRedisAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string interval,
        string? dimensionKey,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc)
    {
        var db = _redis.GetDatabase();
        string sortedSetKey = BuildTimeSeriesKey(tenantId, projectId, metric, dimensionKey, interval);
        string hashKey = BuildTimeSeriesHashKey(tenantId, projectId, metric, dimensionKey, interval);

        if (!await db.KeyExistsAsync(sortedSetKey).ConfigureAwait(false))
            return null;

        var members = await db.SortedSetRangeByRankAsync(sortedSetKey).ConfigureAwait(false);
        if (members.Length == 0)
            return null;

        var result = new List<TimeSeriesPoint>();
        foreach (var member in members)
        {
            if (long.TryParse((string?)member, out long timestamp))
            {
                var ts = DateTimeOffset.FromUnixTimeSeconds(timestamp);

                if (fromUtc.HasValue && ts < fromUtc.Value) continue;
                if (toUtc.HasValue && ts > toUtc.Value) continue;

                var value = await db.HashGetAsync(hashKey, member).ConfigureAwait(false);
                if (value.HasValue && long.TryParse((string?)value, out long count))
                {
                    result.Add(new TimeSeriesPoint
                    {
                        Timestamp = ts,
                        Value = count
                    });
                }
            }
        }

        return result;
    }

    private async Task<List<TimeSeriesPoint>> GetFromSqlAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string interval,
        string? dimensionKey,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken ct)
    {
        var query = _db.AggregateBuckets
            .AsNoTracking()
            .TagWith("MetricsService.GetFromSql")
            .Where(b => b.TenantId == tenantId
                && b.ProjectId == projectId
                && b.Metric == metric
                && b.Interval == "1m");

        // If dimension specified, filter by it; otherwise return all (no dimension filter)
        if (dimensionKey is not null)
            query = query.Where(b => b.DimensionKey == dimensionKey);

        if (toUtc.HasValue)
            query = query.Where(b => b.BucketStartUtc <= toUtc.Value);

        // Apply time range filter - use provided fromUtc or default based on interval
        if (fromUtc.HasValue)
        {
            query = query.Where(b => b.BucketStartUtc >= fromUtc.Value);
        }
        else
        {
            // Apply default cutoff based on interval
            var cutoff = interval switch
            {
                "60s" => DateTimeOffset.UtcNow.AddMinutes(-1),
                "60m" => DateTimeOffset.UtcNow.AddMinutes(-60),
                _ => DateTimeOffset.UtcNow.AddHours(-24)
            };
            query = query.Where(b => b.BucketStartUtc >= cutoff);
        }

        // Apply aggregation based on interval
        if (interval == "24h")
        {
            // Aggregate by hour for 24h view
            return await query
                .GroupBy(b => new { b.BucketStartUtc.Year, b.BucketStartUtc.Month, b.BucketStartUtc.Day, b.BucketStartUtc.Hour })
                .Select(g => new TimeSeriesPoint
                {
                    Timestamp = new DateTimeOffset(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0, TimeSpan.Zero),
                    Value = g.Sum(b => b.Value)
                })
                .OrderBy(p => p.Timestamp)
                .ToListAsync(ct).ConfigureAwait(false);
        }

        // For 60s and 60m, return raw 1-minute buckets
        return await query
            .OrderBy(b => b.BucketStartUtc)
            .Select(b => new TimeSeriesPoint { Timestamp = b.BucketStartUtc, Value = b.Value })
            .ToListAsync(ct).ConfigureAwait(false);
    }

    private static (DateTimeOffset from, DateTimeOffset to) GetTimeRangeForInterval(string interval)
    {
        var now = DateTimeOffset.UtcNow;
        return interval switch
        {
            "60s" => (now.AddMinutes(-1), now),
            "60m" => (now.AddMinutes(-60), now),
            "24h" => (now.AddHours(-24), now),
            _ => (now.AddMinutes(-60), now)
        };
    }

    private static string BuildTimeSeriesKey(Guid tenantId, Guid projectId, string metric, string? dimensionKey, string interval)
    {
        var dim = string.IsNullOrEmpty(dimensionKey) ? "" : $":{dimensionKey}";
        return $"ts:{tenantId}:{projectId}:{metric}{dim}:{interval}";
    }

    private static string BuildTimeSeriesHashKey(Guid tenantId, Guid projectId, string metric, string? dimensionKey, string interval)
    {
        var dim = string.IsNullOrEmpty(dimensionKey) ? "" : $":{dimensionKey}";
        return $"tsh:{tenantId}:{projectId}:{metric}{dim}:{interval}";
    }
}
