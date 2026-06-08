using DocPrinter.Core.Models;

namespace DocPrinter.Core.Abstractions;

/// <summary>
/// Prints a document double-sided on a single-sided printer using the manual flip workflow:
/// print the even pages, ask the user to flip the stack, then print the odd pages onto it.
/// Implemented in DocPrinter.Printing.
/// </summary>
public interface IDuplexPrinter
{
    /// <summary>
    /// Prints <paramref name="file"/> in two passes. After the first (even) pass completes,
    /// <paramref name="confirmFlip"/> is awaited so the UI can prompt the user to flip the
    /// stack; the second (odd) pass runs once it resolves.
    /// </summary>
    /// <param name="file">The (cleaned) document PDF to print.</param>
    /// <param name="confirmFlip">
    /// Invoked between passes. Should resolve to <c>true</c> when the user has flipped the
    /// stack and is ready to continue, or <c>false</c> to abort the second pass.
    /// </param>
    /// <param name="selection">
    /// Which AYT sections to print. <c>null</c> (the default) prints the whole document; ignored for
    /// TYT. The answer key and cover pages are always printed regardless of this value.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PipelineResult> PrintDuplexAsync(
        DocumentFile file,
        Func<CancellationToken, Task<bool>> confirmFlip,
        PrintSelection? selection = null,
        CancellationToken cancellationToken = default);
}
