using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Microsoft.AspNetCore.Mvc;

using PulseBoard.Api.Models.Request;
using PulseBoard.Api.Validation;
using PulseBoard.Contracts.Messaging;

namespace PulseBoard.Api.Controllers;

[ApiController]
[Route("events")]
public sealed class EventsController : ControllerBase
{
    private readonly ServiceBusSender _sender;

    public EventsController(ServiceBusSender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] IncomingEventDto dto, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;

        if (!IncomingEventDtoValidator.TryValidate(dto, out var error))
            return BadRequest(new { error });

        var msg = new IncomingEventMessage
        {
            EventId = dto.EventId.Trim(),
            TenantId = tenantId,
            ProjectKey = dto.ProjectKey.Trim(),
            Timestamp = dto.Timestamp,
            Payload = dto.Payload?.ValueKind == JsonValueKind.Undefined ? null : dto.Payload?.GetRawText(),
            DimensionKey = FormatDimensionKey(dto.Dimensions)
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(msg);

        var sbMessage = new ServiceBusMessage(body)
        {
            MessageId = msg.EventId,
            ContentType = "application/json"
        };

        sbMessage.ApplicationProperties["tenantId"] = msg.TenantId.ToString();
        sbMessage.ApplicationProperties["projectKey"] = msg.ProjectKey;

        await _sender.SendMessageAsync(sbMessage, ct).ConfigureAwait(false);

        return Accepted(new { eventId = msg.EventId });
    }

    private static string? FormatDimensionKey(IReadOnlyList<DimensionDto>? dimensions)
    {
        if (dimensions is null || dimensions.Count == 0)
            return null;

        var dim = dimensions[0];
#pragma warning disable CA1308 // Normalize strings to uppercase - lowercase is intentional for dimension values
        return $"{dim.Key}:{dim.Value.ToLowerInvariant()}";
#pragma warning restore CA1308
    }
}
