using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using DocPrinter.Core.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Watermark;

/// <summary>
/// Removes the diagonal text-overlay watermark by emptying the content stream of every
/// watermark Form XObject found by <see cref="WatermarkDetector"/>. The page content and the
/// answer key are untouched — only the rotated overlay artwork is neutralized, so the result is
/// a byte-faithful document minus the stamp.
/// </summary>
public sealed class PdfWatermarkRemover(ILogger<PdfWatermarkRemover> logger) : IWatermarkRemover
{
    public Task<DocumentFile> RemoveAsync(
        DocumentFile source,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string destination = Path.GetFullPath(destinationPath);
        string? directory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using PdfDocument document = PdfReader.Open(source.Path, PdfDocumentOpenMode.Modify);

        IReadOnlyList<PdfDictionary> watermarks = WatermarkDetector.Find(document);
        if (watermarks.Count == 0)
        {
            throw new WatermarkNotFoundException(source.Path);
        }

        foreach (PdfDictionary watermark in watermarks)
        {
            EmptyStream(watermark);
        }

        logger.LogInformation(
            "Removed {Count} watermark form(s) from {Source} → {Destination}",
            watermarks.Count, source.Path, destination);

        document.Save(destination);

        return Task.FromResult(source with { Path = destination, IsWatermarkRemoved = true });
    }

    // Replace the form's content with nothing. Drop /Filter so the now-empty bytes are not
    // interpreted as a compressed stream; PDFsharp recomputes /Length on save.
    private static void EmptyStream(PdfDictionary dict)
    {
        dict.Elements.Remove("/Filter");
        dict.Stream!.Value = Array.Empty<byte>();
        dict.Elements.SetInteger("/Length", 0);
    }
}
