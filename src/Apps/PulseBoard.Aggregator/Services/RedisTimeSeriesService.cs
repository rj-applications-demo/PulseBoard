using System.Globalization;

using PulseBoard.Contracts.Messaging;

using StackExchange.Redis;

namespace PulseBoard.Aggregator.Services;

public sealed class RedisTimeSeriesService : IRedisTimeSeriesService
{
    private const int SecondCounterTtlSeconds = 120;
    private const int SixtySecondSeriesTtlSeconds = 120;
    private const int SixtyMinuteSeriesTtlSeconds = 7200; // 2 hours
    private const int TwentyFourHourSeriesTtlSeconds = 93600; // 26 hours

    private readonly IConnectionMultiplexer _redis;

    public RedisTimeSeriesService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task IncrementSecondCounterAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string? dimensionKey,
        DateTimeOffset eventTimestamp,
        CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        long unixSecond = eventTimestamp.ToUnixTimeSeconds();
        string key = BuildSecondCounterKey(tenantId, projectId, metric, dimensionKey, unixSecond);

        await db.StringIncrementAsync(key).ConfigureAwait(false);
        await db.KeyExpireAsync(key, TimeSpan.FromSeconds(SecondCounterTtlSeconds)).ConfigureAwait(false);
    }

    public async Task RebuildSixtySecondSeriesAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string? dimensionKey,
        CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow;
        var startSecond = now.AddSeconds(-60).ToUnixTimeSeconds();
        var endSecond = now.ToUnixTimeSeconds();

        var entries = new List<SortedSetEntry>();

        for (long sec = startSecond; sec <= endSecond; sec++)
        {
            string counterKey = BuildSecondCounterKey(tenantId, projectId, metric, dimensionKey, sec);
            var value = await db.StringGetAsync(counterKey).ConfigureAwait(false);

            if (value.HasValue && long.TryParse((string?)value, out long count) && count > 0)
            {
                string secStr = sec.ToString(CultureInfo.InvariantCulture);
                entries.Add(new SortedSetEntry(secStr, sec));
                // Store the actual count in a hash for retrieval
                string hashKey = BuildTimeSeriesHashKey(tenantId, projectId, metric, dimensionKey, "60s");
                await db.HashSetAsync(hashKey, secStr, count).ConfigureAwait(false);
                await db.KeyExpireAsync(hashKey, TimeSpan.FromSeconds(SixtySecondSeriesTtlSeconds)).ConfigureAwait(false);
            }
        }

        string sortedSetKey = BuildTimeSeriesKey(tenantId, projectId, metric, dimensionKey, "60s");

        if (entries.Count > 0)
        {
            // Clear old entries outside window
            await db.SortedSetRemoveRangeByScoreAsync(sortedSetKey, double.NegativeInfinity, startSecond - 1).ConfigureAwait(false);
            await db.SortedSetAddAsync(sortedSetKey, entries.ToArray()).ConfigureAwait(false);
            await db.KeyExpireAsync(sortedSetKey, TimeSpan.FromSeconds(SixtySecondSeriesTtlSeconds)).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<TimeSeriesDataPoint>> GetTimeSeriesAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string? dimensionKey,
        string interval,
        CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        string sortedSetKey = BuildTimeSeriesKey(tenantId, projectId, metric, dimensionKey, interval);
        string hashKey = BuildTimeSeriesHashKey(tenantId, projectId, metric, dimensionKey, interval);

        var members = await db.SortedSetRangeByRankAsync(sortedSetKey).ConfigureAwait(false);
        var result = new List<TimeSeriesDataPoint>();

        foreach (var member in members)
        {
            if (long.TryParse((string?)member, out long timestamp))
            {
                var value = await db.HashGetAsync(hashKey, member).ConfigureAwait(false);
                if (value.HasValue && long.TryParse((string?)value, out long count))
                {
                    result.Add(new TimeSeriesDataPoint
                    {
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp),
                        Value = count
                    });
                }
            }
        }

        return result;
    }

    public async Task UpdateLongIntervalSeriesAsync(
        Guid tenantId,
        Guid projectId,
        string metric,
        string? dimensionKey,
        string interval,
        IReadOnlyList<TimeSeriesDataPoint> dataPoints,
        CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        string sortedSetKey = BuildTimeSeriesKey(tenantId, projectId, metric, dimensionKey, interval);
        string hashKey = BuildTimeSeriesHashKey(tenantId, projectId, metric, dimensionKey, interval);

        int ttl = interval == "60m" ? SixtyMinuteSeriesTtlSeconds : TwentyFourHourSeriesTtlSeconds;

        var entries = new List<SortedSetEntry>();
        foreach (var dp in dataPoints)
        {
            long unixTime = dp.Timestamp.ToUnixTimeSeconds();
            string unixTimeStr = unixTime.ToString(CultureInfo.InvariantCulture);
            entries.Add(new SortedSetEntry(unixTimeStr, unixTime));
            await db.HashSetAsync(hashKey, unixTimeStr, dp.Value).ConfigureAwait(false);
        }

        if (entries.Count > 0)
        {
            await db.SortedSetAddAsync(sortedSetKey, entries.ToArray()).ConfigureAwait(false);
            await db.KeyExpireAsync(sortedSetKey, TimeSpan.FromSeconds(ttl)).ConfigureAwait(false);
            await db.KeyExpireAsync(hashKey, TimeSpan.FromSeconds(ttl)).ConfigureAwait(false);
        }
    }

    private static string BuildSecondCounterKey(Guid tenantId, Guid projectId, string metric, string? dimensionKey, long unixSecond)
    {
        var dim = string.IsNullOrEmpty(dimensionKey) ? "" : $":{dimensionKey}";
        return $"cnt:{tenantId}:{projectId}:{metric}{dim}:{unixSecond}";
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
