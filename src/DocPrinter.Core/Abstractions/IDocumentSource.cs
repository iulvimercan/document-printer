using DocPrinter.Core.Models;

namespace DocPrinter.Core.Abstractions;

/// <summary>
/// Locates and downloads a past document PDF from its source (the source site).
/// Implemented in DocPrinter.Downloader using browser automation.
/// </summary>
public interface IDocumentSource
{
    /// <summary>
    /// Finds the document matching <paramref name="request"/> at the source and downloads it.
    /// </summary>
    /// <param name="request">Which document to fetch.</param>
    /// <param name="destinationDirectory">Folder to save the PDF into.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The downloaded file, including its source URL.</returns>
    Task<DocumentFile> DownloadAsync(
        DocumentRequest request,
        string destinationDirectory,
        CancellationToken cancellationToken = default);
}
