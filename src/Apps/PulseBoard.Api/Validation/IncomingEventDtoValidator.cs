using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using PulseBoard.Api.Models.Request;

namespace PulseBoard.Api.Validation;

public static partial class IncomingEventDtoValidator
{
    private const int MaxDimensionValueLength = 64;

    [GeneratedRegex("^[a-z0-9._-]+$", RegexOptions.Compiled)]
    private static partial Regex DimensionValuePattern();

    public static bool TryValidate(IncomingEventDto dto, [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(dto.EventId))
        {
            error = "EventId is required.";
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

        if (!TryValidateDimensions(dto.Dimensions, out error))
        {
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryValidateDimensions(
        IReadOnlyList<DimensionDto>? dimensions,
        [NotNullWhen(false)] out string? error)
    {
        if (dimensions is null || dimensions.Count == 0)
        {
            error = null;
            return true;
        }

        if (dimensions.Count > 1)
        {
            error = "Only one dimension is allowed.";
            return false;
        }

        var dimension = dimensions[0];

        if (string.IsNullOrWhiteSpace(dimension.Key))
        {
            error = "Dimension key is required.";
            return false;
        }

        if (dimension.Key != "type")
        {
            error = "Only 'type' dimension key is supported.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dimension.Value))
        {
            error = "Dimension value is required.";
            return false;
        }

#pragma warning disable CA1308 // Normalize strings to uppercase - lowercase is intentional for dimension values
        var normalizedValue = dimension.Value.ToLowerInvariant();
#pragma warning restore CA1308

        if (normalizedValue.Length > MaxDimensionValueLength)
        {
            error = $"Dimension value exceeds {MaxDimensionValueLength} characters.";
            return false;
        }

        if (!DimensionValuePattern().IsMatch(normalizedValue))
        {
            error = "Dimension value contains invalid characters. Only lowercase letters, numbers, dots, underscores, and hyphens are allowed.";
            return false;
        }

        error = null;
        return true;
    }
}
