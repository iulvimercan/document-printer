using DocPrinter.Agent.Abstractions;

namespace DocPrinter.Agent;

/// <summary>
/// Default <see cref="IPipelineStatus"/>: stores the latest activity and raises <see cref="Changed"/>
/// on every report. Writes can arrive on threadpool continuations (the SK/Ollama call chain), so a
/// UI subscriber must marshal the event back onto its own context (e.g. Blazor's
/// <c>InvokeAsync(StateHasChanged)</c>).
/// </summary>
public sealed class PipelineStatus : IPipelineStatus
{
    public PipelineActivity Current { get; private set; } = PipelineActivity.Idle;

    public event Action? Changed;

    public void Report(PipelineActivity value)
    {
        Current = value;
        Changed?.Invoke();
    }
}
