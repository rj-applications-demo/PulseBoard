using PulseBoard.Api.Models.Request;

namespace PulseBoard.Api.Validation;

public static class IncomingEventDtoValidator
{
    public static bool TryValidate(IncomingEventDto dto, out string error)
    {
        if (string.IsNullOrWhiteSpace(dto.EventId))
        {
            error = "EventId is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.TenantId))
        {
            error = "TenantId is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.ProjectKey))
        {
            error = "ProjectKey is required.";
            return false;
        }

        if (dto.Timestamp == default)
        {
            error = "Timestamp is required.";
            return false;
        }

        error = "";
        return true;
    }
}
