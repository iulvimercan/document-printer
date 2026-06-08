using DocPrinter.Agent.Abstractions;

namespace DocPrinter.Agent.Console;

/// <summary>
/// Console implementation of the guided-flip prompt: between the even and odd print passes it asks
/// the operator to flip the paper stack and press Enter (or type 'n' to abort the second pass).
/// </summary>
public sealed class ConsoleFlipPrompt : IFlipPrompt
{
    public Task<bool> ConfirmFlipAsync(CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine();
        System.Console.WriteLine(
            "↻ Even pages printed. Flip the printed stack and reload it, then press Enter to print " +
            "the odd pages (or type 'n' to cancel).");
        System.Console.Write("> ");
        string? answer = System.Console.ReadLine();
        bool proceed = answer is null || !answer.Trim().StartsWith('n');
        return Task.FromResult(proceed);
    }
}
