namespace DocPrinter.Core.Models;

/// <summary>
/// A PDF on disk produced by a pipeline stage, tagged with where it came from
/// and whether the watermark has been removed yet.
/// </summary>
/// <param name="Path">Absolute path to the PDF file on the local machine.</param>
/// <param name="Request">The document this file corresponds to.</param>
/// <param name="SourceUrl">The source URL the file was downloaded from, if known.</param>
/// <param name="IsWatermarkRemoved">True once the watermark-removal stage has processed it.</param>
public sealed record DocumentFile(
    string Path,
    DocumentRequest Request,
    string? SourceUrl = null,
    bool IsWatermarkRemoved = false);
