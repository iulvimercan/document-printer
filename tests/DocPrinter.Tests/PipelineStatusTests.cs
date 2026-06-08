using DocPrinter.Agent;
using DocPrinter.Agent.Abstractions;

namespace DocPrinter.Tests;

public class PipelineStatusTests
{
    [Fact]
    public void Starts_idle()
    {
        var status = new PipelineStatus();
        Assert.Equal(PipelineActivity.Idle, status.Current);
    }

    [Fact]
    public void Report_updates_current_and_raises_changed()
    {
        var status = new PipelineStatus();
        int raised = 0;
        status.Changed += () => raised++;

        status.Report(PipelineActivity.Downloading);

        Assert.Equal(PipelineActivity.Downloading, status.Current);
        Assert.Equal(1, raised);
    }
}
