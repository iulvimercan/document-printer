using DocPrinter.Agent.Abstractions;
using DocPrinter.Web;

namespace DocPrinter.Tests;

public class BlazorFlipPromptTests
{
    [Fact]
    public async Task Confirm_completes_the_awaited_task_and_reports_odd_pass()
    {
        var progress = new CapturingProgress();
        var prompt = new BlazorFlipPrompt(progress);

        Task<bool> pending = prompt.ConfirmFlipAsync();

        Assert.False(pending.IsCompleted);
        Assert.True(prompt.IsAwaitingFlip);
        Assert.Equal(PipelineActivity.AwaitingFlip, progress.Reports.Single());

        prompt.Confirm();

        Assert.True(await pending);
        Assert.False(prompt.IsAwaitingFlip);
        Assert.Equal(
            new[] { PipelineActivity.AwaitingFlip, PipelineActivity.PrintingOddPages },
            progress.Reports);
    }

    [Fact]
    public async Task Cancel_resolves_false_and_reports_failed()
    {
        var progress = new CapturingProgress();
        var prompt = new BlazorFlipPrompt(progress);

        Task<bool> pending = prompt.ConfirmFlipAsync();
        prompt.Cancel();

        Assert.False(await pending);
        Assert.False(prompt.IsAwaitingFlip);
        Assert.Equal(
            new[] { PipelineActivity.AwaitingFlip, PipelineActivity.Failed },
            progress.Reports);
    }

    [Fact]
    public void Confirm_with_no_pending_flip_is_a_no_op()
    {
        var progress = new CapturingProgress();
        var prompt = new BlazorFlipPrompt(progress);

        prompt.Confirm();

        Assert.False(prompt.IsAwaitingFlip);
        Assert.Empty(progress.Reports);
    }
}
