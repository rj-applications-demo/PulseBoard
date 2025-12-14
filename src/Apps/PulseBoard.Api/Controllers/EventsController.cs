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
        if (!IncomingEventDtoValidator.TryValidate(dto, out var error))
            return BadRequest(new { error });

        var msg = new IncomingEventMessage
        {
            EventId = dto.EventId.Trim(),
            TenantId = dto.TenantId.Trim(),
            ProjectKey = dto.ProjectKey.Trim(),
            Timestamp = dto.Timestamp,
            Payload = dto.Payload
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(msg);

        var sbMessage = new ServiceBusMessage(body)
        {
            MessageId = msg.EventId,
            ContentType = "application/json"
        };

        sbMessage.ApplicationProperties["tenantId"] = msg.TenantId;
        sbMessage.ApplicationProperties["projectKey"] = msg.ProjectKey;

        await _sender.SendMessageAsync(sbMessage, ct).ConfigureAwait(false);

        return Accepted(new { eventId = msg.EventId });
    }
}
