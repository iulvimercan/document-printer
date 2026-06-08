namespace DocPrinter.Watermark;

/// <summary>
/// Thrown when no watermark could be located in a PDF that was expected to carry one.
/// The pipeline fails loudly rather than risk printing a still-watermarked document.
/// </summary>
public sealed class WatermarkNotFoundException(string sourcePath)
    : Exception(
        $"No watermark was found in '{sourcePath}'. The PDF layout may have changed; " +
        "refusing to continue so a watermarked document is never printed.")
{
    /// <summary>The PDF that was inspected.</summary>
    public string SourcePath { get; } = sourcePath;
}
