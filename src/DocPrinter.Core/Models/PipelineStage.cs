namespace DocPrinter.Core.Models;

/// <summary>
/// The ordered stages of the end-to-end pipeline.
/// </summary>
public enum PipelineStage
{
    /// <summary>Agent has located and downloaded the document PDF from source.</summary>
    Download,

    /// <summary>The text-overlay watermark has been removed.</summary>
    WatermarkRemoval,

    /// <summary>Even pages printed, awaiting the user to flip the stack.</summary>
    PrintingEvenPages,

    /// <summary>Odd pages printed onto the flipped stack; job complete.</summary>
    PrintingOddPages,
}
