using DocPrinter.Agent.Abstractions;

namespace DocPrinter.Web;

/// <summary>
/// The Blazor implementation of <see cref="IFlipPrompt"/>. The duplex printer awaits
/// <see cref="ConfirmFlipAsync"/> from inside the agent's tool call (which the chat page itself
/// awaits), so the circuit stays responsive: this prompt parks on a <see cref="TaskCompletionSource{TResult}"/>,
/// raises <see cref="Changed"/> for the page to show the flip panel, and resumes when the operator
/// clicks <see cref="Confirm"/> or <see cref="Cancel"/>. Scoped per circuit.
/// </summary>
public sealed class BlazorFlipPrompt(IProgress<PipelineActivity> progress) : IFlipPrompt
{
    private TaskCompletionSource<bool>? _pending;

    /// <summary>True while the operator is being asked to flip the stack.</summary>
    public bool IsAwaitingFlip => _pending is not null;

    /// <summary>Raised when the flip prompt opens or closes, so the page can re-render.</summary>
    public event Action? Changed;

    public Task<bool> ConfirmFlipAsync(CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = tcs;
        cancellationToken.Register(() => Resolve(false, PipelineActivity.Failed));
        progress.Report(PipelineActivity.AwaitingFlip);
        Changed?.Invoke();
        return tcs.Task;
    }

    /// <summary>The operator flipped the stack and is ready for the odd pass.</summary>
    public void Confirm() => Resolve(true, PipelineActivity.PrintingOddPages);

    /// <summary>The operator cancelled the second pass.</summary>
    public void Cancel() => Resolve(false, PipelineActivity.Failed);

    private void Resolve(bool proceed, PipelineActivity next)
    {
        TaskCompletionSource<bool>? pending = _pending;
        if (pending is null)
        {
            return;
        }

        _pending = null;
        progress.Report(next);
        Changed?.Invoke();
        pending.TrySetResult(proceed);
    }
}
