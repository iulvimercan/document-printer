using DocPrinter.Core.Models;

namespace DocPrinter.Core.Abstractions;

/// <summary>
/// Inspects a document PDF and reports its page structure (<see cref="DocumentLayout"/>): the AYT
/// section ranges (empty for TYT), plus the answer-key and instructions page positions.
/// Implemented in DocPrinter.Printing.
/// </summary>
public interface ISectionDetector
{
    /// <summary>Detects the page layout of <paramref name="file"/>.</summary>
    Task<DocumentLayout> DetectAsync(DocumentFile file, CancellationToken cancellationToken = default);
}
