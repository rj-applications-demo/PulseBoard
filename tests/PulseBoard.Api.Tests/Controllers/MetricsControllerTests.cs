using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using PulseBoard.Api.Controllers;
using PulseBoard.Api.Models.Response;
using PulseBoard.Api.Services;

namespace PulseBoard.Api.Tests.Controllers;

public sealed class MetricsControllerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    private static MetricsController CreateController(IMetricsService metricsService)
    {
        var controller = new MetricsController(metricsService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Items["TenantId"] = TestTenantId;
        return controller;
    }

    private static TimeSeriesResponse CreateTimeSeriesResponse()
    {
        return new TimeSeriesResponse
        {
            ProjectKey = "proj-1",
            Metric = "event.count",
            Interval = "60s",
            Source = "redis",
            DataPoints = []
        };
    }

    private static TopProjectsResponse CreateTopProjectsResponse()
    {
        return new TopProjectsResponse
        {
            Metric = "event.count",
            Interval = "60m",
            Source = "sql",
            Projects = []
        };
    }

    // --- GetTimeSeries tests ---

    [Fact]
    public async Task GetTimeSeries_ValidParams_ReturnsOk()
    {
        var service = Substitute.For<IMetricsService>();
        service.GetTimeSeriesAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(CreateTimeSeriesResponse());
        var controller = CreateController(service);

        var result = await controller.GetTimeSeries("proj-1", ct: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetTimeSeries_EmptyProjectKey_ReturnsBadRequest(string projectKey)
    {
        var service = Substitute.For<IMetricsService>();
        var controller = CreateController(service);

        var result = await controller.GetTimeSeries(projectKey, ct: CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("5m")]
    [InlineData("1h")]
    [InlineData("")]
    public async Task GetTimeSeries_InvalidInterval_ReturnsBadRequest(string interval)
    {
        var service = Substitute.For<IMetricsService>();
        var controller = CreateController(service);

        var result = await controller.GetTimeSeries("proj-1", interval: interval, ct: CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("60s")]
    [InlineData("60m")]
    [InlineData("24h")]
    public async Task GetTimeSeries_ValidIntervals_ReturnsOk(string interval)
    {
        var service = Substitute.For<IMetricsService>();
        service.GetTimeSeriesAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(CreateTimeSeriesResponse());
        var controller = CreateController(service);

        var result = await controller.GetTimeSeries("proj-1", interval: interval, ct: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetTimeSeries_ProjectNotFound_ReturnsNotFound()
    {
        var service = Substitute.For<IMetricsService>();
        service.GetTimeSeriesAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns((TimeSeriesResponse?)null);
        var controller = CreateController(service);

        var result = await controller.GetTimeSeries("proj-1", ct: CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetTimeSeries_PassesTrimmedProjectKey()
    {
        var service = Substitute.For<IMetricsService>();
        service.GetTimeSeriesAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(CreateTimeSeriesResponse());
        var controller = CreateController(service);

        await controller.GetTimeSeries("  proj-1  ", ct: CancellationToken.None);

        await service.Received(1).GetTimeSeriesAsync(
            TestTenantId,
            "proj-1",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>());
    }

    // --- GetTopProjects tests ---

    [Fact]
    public async Task GetTopProjects_ValidParams_ReturnsOk()
    {
        var service = Substitute.For<IMetricsService>();
        service.GetTopProjectsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateTopProjectsResponse());
        var controller = CreateController(service);

        var result = await controller.GetTopProjects(ct: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Theory]
    [InlineData("5m")]
    [InlineData("1h")]
    public async Task GetTopProjects_InvalidInterval_ReturnsBadRequest(string interval)
    {
        var service = Substitute.For<IMetricsService>();
        var controller = CreateController(service);

        var result = await controller.GetTopProjects(interval: interval, ct: CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetTopProjects_InvalidLimit_ReturnsBadRequest(int limit)
    {
        var service = Substitute.For<IMetricsService>();
        var controller = CreateController(service);

        var result = await controller.GetTopProjects(limit: limit, ct: CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task GetTopProjects_BoundaryLimits_ReturnsOk(int limit)
    {
        var service = Substitute.For<IMetricsService>();
        service.GetTopProjectsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateTopProjectsResponse());
        var controller = CreateController(service);

        var result = await controller.GetTopProjects(limit: limit, ct: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetTopProjects_PassesCorrectTenantId()
    {
        var service = Substitute.For<IMetricsService>();
        service.GetTopProjectsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateTopProjectsResponse());
        var controller = CreateController(service);

        await controller.GetTopProjects(ct: CancellationToken.None);

        await service.Received(1).GetTopProjectsAsync(
            TestTenantId,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }
}
