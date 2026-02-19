using PulseBoard.Api.Models.Request;
using PulseBoard.Api.Validation;

namespace PulseBoard.Api.Tests.Validation;

public sealed class IncomingEventDtoValidatorTests
{
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
    public void TryValidate_ValidDto_ReturnsTrue()
    {
        var dto = CreateValidDto();

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.True(result);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_NullEventId_ReturnsFalse()
    {
        var dto = CreateValidDto(eventId: null!);

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Equal("EventId is required.", error);
    }

    [Fact]
    public void TryValidate_WhitespaceEventId_ReturnsFalse()
    {
        var dto = CreateValidDto(eventId: "   ");

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Equal("EventId is required.", error);
    }

    [Fact]
    public void TryValidate_NullProjectKey_ReturnsFalse()
    {
        var dto = CreateValidDto(projectKey: null!);

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Equal("ProjectKey is required.", error);
    }

    [Fact]
    public void TryValidate_WhitespaceProjectKey_ReturnsFalse()
    {
        var dto = CreateValidDto(projectKey: "   ");

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Equal("ProjectKey is required.", error);
    }

    [Fact]
    public void TryValidate_DefaultTimestamp_ReturnsFalse()
    {
        var dto = new IncomingEventDto
        {
            EventId = "evt-1",
            ProjectKey = "proj-1",
            Timestamp = default
        };

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Equal("Timestamp is required.", error);
    }

    [Fact]
    public void TryValidate_NullDimensions_ReturnsTrue()
    {
        var dto = CreateValidDto(dimensions: null);

        var result = IncomingEventDtoValidator.TryValidate(dto, out _);

        Assert.True(result);
    }

    [Fact]
    public void TryValidate_EmptyDimensions_ReturnsTrue()
    {
        var dto = CreateValidDto(dimensions: []);

        var result = IncomingEventDtoValidator.TryValidate(dto, out _);

        Assert.True(result);
    }

    [Fact]
    public void TryValidate_MultipleDimensions_ReturnsFalse()
    {
        var dims = new List<DimensionDto>
        {
            new() { Key = "type", Value = "click" },
            new() { Key = "type", Value = "view" }
        };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Equal("Only one dimension is allowed.", error);
    }

    [Fact]
    public void TryValidate_DimensionKeyNull_ReturnsFalse()
    {
        var dims = new List<DimensionDto> { new() { Key = null!, Value = "click" } };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Equal("Dimension key is required.", error);
    }

    [Fact]
    public void TryValidate_DimensionKeyNotType_ReturnsFalse()
    {
        var dims = new List<DimensionDto> { new() { Key = "category", Value = "click" } };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Equal("Only 'type' dimension key is supported.", error);
    }

    [Fact]
    public void TryValidate_DimensionValueNull_ReturnsFalse()
    {
        var dims = new List<DimensionDto> { new() { Key = "type", Value = null! } };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Equal("Dimension value is required.", error);
    }

    [Fact]
    public void TryValidate_DimensionValueTooLong_ReturnsFalse()
    {
        var longValue = new string('a', 65);
        var dims = new List<DimensionDto> { new() { Key = "type", Value = longValue } };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Contains("exceeds 64 characters", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryValidate_DimensionValueExactly64_ReturnsTrue()
    {
        var exactValue = new string('a', 64);
        var dims = new List<DimensionDto> { new() { Key = "type", Value = exactValue } };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out _);

        Assert.True(result);
    }

    [Theory]
    [InlineData("page.view")]
    [InlineData("button-click")]
    [InlineData("user_signup")]
    [InlineData("abc123")]
    public void TryValidate_DimensionValueValidPatterns_ReturnsTrue(string value)
    {
        var dims = new List<DimensionDto> { new() { Key = "type", Value = value } };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out _);

        Assert.True(result);
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("has@symbol")]
    [InlineData("has/slash")]
    public void TryValidate_DimensionValueInvalidChars_ReturnsFalse(string value)
    {
        var dims = new List<DimensionDto> { new() { Key = "type", Value = value } };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.False(result);
        Assert.Contains("invalid characters", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryValidate_DimensionValueUppercase_NormalizedAndValid()
    {
        var dims = new List<DimensionDto> { new() { Key = "type", Value = "PageView" } };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out _);

        Assert.True(result);
    }

    [Fact]
    public void TryValidate_ValidDtoWithDimension_ReturnsTrue()
    {
        var dims = new List<DimensionDto> { new() { Key = "type", Value = "click" } };
        var dto = CreateValidDto(dimensions: dims);

        var result = IncomingEventDtoValidator.TryValidate(dto, out var error);

        Assert.True(result);
        Assert.Null(error);
    }
}
