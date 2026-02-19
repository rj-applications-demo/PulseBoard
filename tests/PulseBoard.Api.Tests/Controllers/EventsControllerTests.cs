using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using NSubstitute;

using PulseBoard.Api.Controllers;
using PulseBoard.Api.Models.Request;

namespace PulseBoard.Api.Tests.Controllers;

public sealed class EventsControllerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    private static EventsController CreateController(ServiceBusSender sender)
    {
        var controller = new EventsController(sender);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Items["TenantId"] = TestTenantId;
        return controller;
    }

    private static IncomingEventDto CreateValidDto(
        string eventId = "evt-1",
        string projectKey = "proj-1",
        IReadOnlyList<DimensionDto>? dimensions = null)
    {
        return new IncomingEventDto
        {
            EventId = eventId,
            ProjectKey = projectKey,
            Timestamp = DateTimeOffset.UtcNow,
            Dimensions = dimensions
        };
    }

    [Fact]
    public async Task Post_ValidDto_ReturnsAccepted()
    {
        var sender = Substitute.For<ServiceBusSender>();
        var controller = CreateController(sender);
        var dto = CreateValidDto();

        var result = await controller.Post(dto, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        var body = JsonSerializer.SerializeToElement(accepted.Value);
        Assert.Equal("evt-1", body.GetProperty("eventId").GetString());
    }

    [Fact]
    public async Task Post_ValidDto_SendsMessageToServiceBus()
    {
        var sender = Substitute.For<ServiceBusSender>();
        var controller = CreateController(sender);
        var dto = CreateValidDto();

        await controller.Post(dto, CancellationToken.None);

        await sender.Received(1).SendMessageAsync(
            Arg.Any<ServiceBusMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_InvalidDto_ReturnsBadRequest()
    {
        var sender = Substitute.For<ServiceBusSender>();
        var controller = CreateController(sender);
        var dto = CreateValidDto(eventId: null!);

        var result = await controller.Post(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Post_InvalidDto_DoesNotSendToServiceBus()
    {
        var sender = Substitute.For<ServiceBusSender>();
        var controller = CreateController(sender);
        var dto = CreateValidDto(eventId: null!);

        await controller.Post(dto, CancellationToken.None);

        await sender.DidNotReceive().SendMessageAsync(
            Arg.Any<ServiceBusMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_ValidDto_TrimsEventIdAndProjectKey()
    {
        var sender = Substitute.For<ServiceBusSender>();
        ServiceBusMessage? captured = null;
        await sender.SendMessageAsync(
            Arg.Do<ServiceBusMessage>(m => captured = m),
            Arg.Any<CancellationToken>());
        var controller = CreateController(sender);
        var dto = CreateValidDto(eventId: "  evt-1  ", projectKey: "  proj  ");

        var result = await controller.Post(dto, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        var body = JsonSerializer.SerializeToElement(accepted.Value);
        Assert.Equal("evt-1", body.GetProperty("eventId").GetString());
        Assert.NotNull(captured);
        Assert.Equal("evt-1", captured!.MessageId);
    }

    [Fact]
    public async Task Post_WithDimension_FormatsDimensionKeyLowercased()
    {
        var sender = Substitute.For<ServiceBusSender>();
        ServiceBusMessage? captured = null;
        await sender.SendMessageAsync(
            Arg.Do<ServiceBusMessage>(m => captured = m),
            Arg.Any<CancellationToken>());
        var controller = CreateController(sender);
        var dims = new List<DimensionDto> { new() { Key = "type", Value = "Click" } };
        var dto = CreateValidDto(dimensions: dims);

        await controller.Post(dto, CancellationToken.None);

        Assert.NotNull(captured);
        var msgBody = JsonSerializer.Deserialize<JsonElement>(captured!.Body);
        Assert.Equal("type:click", msgBody.GetProperty("DimensionKey").GetString());
    }

    [Fact]
    public async Task Post_WithoutDimensions_DimensionKeyIsNull()
    {
        var sender = Substitute.For<ServiceBusSender>();
        ServiceBusMessage? captured = null;
        await sender.SendMessageAsync(
            Arg.Do<ServiceBusMessage>(m => captured = m),
            Arg.Any<CancellationToken>());
        var controller = CreateController(sender);
        var dto = CreateValidDto();

        await controller.Post(dto, CancellationToken.None);

        Assert.NotNull(captured);
        var msgBody = JsonSerializer.Deserialize<JsonElement>(captured!.Body);
        Assert.Equal(JsonValueKind.Null, msgBody.GetProperty("DimensionKey").ValueKind);
    }

    [Fact]
    public async Task Post_ValidDto_SetsCorrectApplicationProperties()
    {
        var sender = Substitute.For<ServiceBusSender>();
        ServiceBusMessage? captured = null;
        await sender.SendMessageAsync(
            Arg.Do<ServiceBusMessage>(m => captured = m),
            Arg.Any<CancellationToken>());
        var controller = CreateController(sender);
        var dto = CreateValidDto(projectKey: "my-project");

        await controller.Post(dto, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(TestTenantId.ToString(), captured!.ApplicationProperties["tenantId"]);
        Assert.Equal("my-project", captured.ApplicationProperties["projectKey"]);
    }
}
