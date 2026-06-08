using DocPrinter.Core.Models;

namespace DocPrinter.Core.Abstractions;

/// <summary>
/// Removes the diagonal text-overlay watermark from a document PDF, producing a clean copy.
/// Implemented by the custom DocPrinter.Watermark project.
/// </summary>
public interface IWatermarkRemover
{
    /// <summary>
    /// Reads <paramref name="source"/>, strips the watermark, and writes a clean PDF.
    /// </summary>
    /// <param name="source">The watermarked document file.</param>
    /// <param name="destinationPath">Where to write the cleaned PDF.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cleaned file, with <see cref="DocumentFile.IsWatermarkRemoved"/> set.</returns>
    Task<DocumentFile> RemoveAsync(
        DocumentFile source,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
