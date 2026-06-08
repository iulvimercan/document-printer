namespace DocPrinter.Core.Models;

/// <summary>
/// The outcome of running the full pipeline (download → dewatermark → duplex print)
/// for one document request. Carries enough detail for the UI to report what happened.
/// </summary>
/// <param name="Stage">The stage that was last reached.</param>
/// <param name="Succeeded">True only if every stage through <see cref="Stage"/> completed.</param>
/// <param name="File">The resulting file, if the pipeline got far enough to produce one.</param>
/// <param name="Message">Human-readable detail, e.g. an error reason or a success note.</param>
public sealed record PipelineResult(
    PipelineStage Stage,
    bool Succeeded,
    DocumentFile? File = null,
    string? Message = null);
