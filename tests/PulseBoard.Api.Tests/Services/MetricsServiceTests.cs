using NSubstitute;

using PulseBoard.Api.Services;
using PulseBoard.Api.Tests.Helpers;
using PulseBoard.Infrastructure.Entities;

using StackExchange.Redis;

namespace PulseBoard.Api.Tests.Services;

public sealed class MetricsServiceTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    private static (IConnectionMultiplexer redis, IDatabase redisDb) CreateRedisMock()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (redis, db);
    }

    private static async Task<Project> SeedProjectAsync(Infrastructure.AppDbContext db, string key = "proj-1")
    {
        var tenant = new Tenant { Name = "Test" };
        tenant.GetType().GetProperty("Id")!.SetValue(tenant, TestTenantId);
        db.Tenants.Add(tenant);

        var project = new Project
        {
            TenantId = TestTenantId,
            Key = key,
            Name = key
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    [Fact]
    public async Task GetTimeSeriesAsync_ProjectNotFound_ReturnsNull()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (redis, _) = CreateRedisMock();
        var service = new MetricsService(redis, db);

        var result = await service.GetTimeSeriesAsync(
            TestTenantId, "nonexistent", "event.count", "60s", null, null, null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_RedisHasData_ReturnsRedisSource()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = await SeedProjectAsync(db);
        var (redis, redisDb) = CreateRedisMock();

        // Mock Redis key exists and has data
        redisDb.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);
        redisDb.SortedSetRangeByRankAsync(
            Arg.Any<RedisKey>(), Arg.Any<long>(), Arg.Any<long>(),
            Arg.Any<Order>(), Arg.Any<CommandFlags>())
            .Returns(new RedisValue[] { "1700000000" });
        redisDb.HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"42");

        var service = new MetricsService(redis, db);

        var result = await service.GetTimeSeriesAsync(
            TestTenantId, "proj-1", "event.count", "60s", null, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("redis", result!.Source);
        Assert.Single(result.DataPoints);
        Assert.Equal(42, result.DataPoints[0].Value);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_RedisKeyNotFound_FallsToSql()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = await SeedProjectAsync(db);
        var (redis, redisDb) = CreateRedisMock();
        redisDb.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);

        var service = new MetricsService(redis, db);

        var result = await service.GetTimeSeriesAsync(
            TestTenantId, "proj-1", "event.count", "60s", null, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("sql", result!.Source);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_WrongTenant_ReturnsNull()
    {
        using var db = InMemoryDbContextFactory.Create();
        await SeedProjectAsync(db);
        var (redis, _) = CreateRedisMock();
        var service = new MetricsService(redis, db);

        var result = await service.GetTimeSeriesAsync(
            Guid.NewGuid(), "proj-1", "event.count", "60s", null, null, null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTopProjectsAsync_NoData_ReturnsEmptyList()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (redis, _) = CreateRedisMock();
        var service = new MetricsService(redis, db);

        var result = await service.GetTopProjectsAsync(
            TestTenantId, "event.count", "60m", null, 10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Projects);
    }

    [Fact]
    public async Task GetTopProjectsAsync_WithData_ReturnsOrderedByTotal()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project1 = await SeedProjectAsync(db, "proj-small");

        var project2 = new Project { TenantId = TestTenantId, Key = "proj-large", Name = "proj-large" };
        db.Projects.Add(project2);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        db.AggregateBuckets.Add(new AggregateBucket
        {
            TenantId = TestTenantId,
            ProjectId = project1.Id,
            Metric = "event.count",
            Interval = "1m",
            BucketStartUtc = now.AddMinutes(-5),
            Value = 10
        });
        db.AggregateBuckets.Add(new AggregateBucket
        {
            TenantId = TestTenantId,
            ProjectId = project2.Id,
            Metric = "event.count",
            Interval = "1m",
            BucketStartUtc = now.AddMinutes(-5),
            Value = 100
        });
        await db.SaveChangesAsync();

        var (redis, _) = CreateRedisMock();
        var service = new MetricsService(redis, db);

        var result = await service.GetTopProjectsAsync(
            TestTenantId, "event.count", "60m", null, 10, CancellationToken.None);

        Assert.Equal(2, result.Projects.Count);
        Assert.Equal("proj-large", result.Projects[0].ProjectKey);
        Assert.Equal(100, result.Projects[0].Value);
        Assert.Equal("proj-small", result.Projects[1].ProjectKey);
    }

    [Fact]
    public async Task GetTopProjectsAsync_RespectsLimit()
    {
        using var db = InMemoryDbContextFactory.Create();
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 5; i++)
        {
            var project = new Project { TenantId = TestTenantId, Key = $"proj-{i}", Name = $"Project {i}" };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            db.AggregateBuckets.Add(new AggregateBucket
            {
                TenantId = TestTenantId,
                ProjectId = project.Id,
                Metric = "event.count",
                Interval = "1m",
                BucketStartUtc = now.AddMinutes(-5),
                Value = (i + 1) * 10
            });
        }

        // Seed the tenant (needed for FK in a real DB, InMemory doesn't enforce but let's be consistent)
        var tenant = new Tenant { Name = "Test" };
        tenant.GetType().GetProperty("Id")!.SetValue(tenant, TestTenantId);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var (redis, _) = CreateRedisMock();
        var service = new MetricsService(redis, db);

        var result = await service.GetTopProjectsAsync(
            TestTenantId, "event.count", "60m", null, 2, CancellationToken.None);

        Assert.Equal(2, result.Projects.Count);
    }
}
