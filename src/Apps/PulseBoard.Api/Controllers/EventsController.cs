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
        if (!IncomingEventDtoValidator.TryValidate(dto, out var eventId, out var tenantId, out var error))
            return BadRequest(new { error });

        var msg = new IncomingEventMessage
        {
            EventId = eventId,
            TenantId = tenantId,
            ProjectKey = dto.ProjectKey.Trim(),
            Timestamp = dto.Timestamp,
            Payload = dto.Payload
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(msg);

        var sbMessage = new ServiceBusMessage(body)
        {
            MessageId = msg.EventId.ToString(),
            ContentType = "application/json"
        };

        sbMessage.ApplicationProperties["tenantId"] = msg.TenantId.ToString();
        sbMessage.ApplicationProperties["projectKey"] = msg.ProjectKey;

        await _sender.SendMessageAsync(sbMessage, ct).ConfigureAwait(false);

        return Accepted(new { eventId = msg.EventId });
    }
}
