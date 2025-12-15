using PulseBoard.Domain.Events.Entities;

namespace PulseBoard.Domain.Tests;

public class ProjectTests
{
    [Fact]
    public void RecordEvents_IncrementsTotalEventCount()
    {
        var project = new Project("tenant-123", "My App");

        project.RecordEvents(100);

        Assert.Equal(100, project.TotalEventCount);
    }

    [Fact]
    public void RecordEvents_Throws_OnNonPositiveBatchSize()
    {
        var project = new Project("tenant-123", "My App");

        Assert.Throws<ArgumentOutOfRangeException>(() => project.RecordEvents(0));
    }
}
