namespace DocPrinter.Agent.Abstractions;

/// <summary>
/// Asks the operator to flip the printed stack between the even and odd passes of the guided-flip
/// duplex print. It is the bridge between <c>IDuplexPrinter</c>'s <c>confirmFlip</c> callback and
/// whatever UI is driving the agent (the console harness now, the Blazor circuit in Step 6). The
/// host registers the concrete implementation; the agent project never assumes one.
/// </summary>
public interface IFlipPrompt
{
    /// <summary>
    /// Prompts the operator to flip the stack and resolves to <c>true</c> once they are ready to
    /// print the second pass, or <c>false</c> to abort it.
    /// </summary>
    Task<bool> ConfirmFlipAsync(CancellationToken cancellationToken = default);
}
