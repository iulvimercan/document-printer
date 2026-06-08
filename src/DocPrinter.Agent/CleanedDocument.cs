using DocPrinter.Core.Models;

namespace DocPrinter.Agent;

/// <summary>
/// The output of <see cref="DocumentPipeline.PrepareAsync"/>: a downloaded, watermark-free document PDF
/// together with the page layout the section detector found. Holding both lets the agent offer AYT
/// section choices (from <see cref="Layout"/>) before committing to the print.
/// </summary>
/// <param name="File">The cleaned document PDF (watermark removed).</param>
/// <param name="Layout">The detected page layout — AYT section ranges, answer key, instructions.</param>
public sealed record CleanedDocument(DocumentFile File, DocumentLayout Layout);
