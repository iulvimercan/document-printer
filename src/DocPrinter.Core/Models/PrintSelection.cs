namespace DocPrinter.Core.Models;

/// <summary>
/// Which AYT sections the user wants printed. Ignored for TYT (all sections are always printed).
/// The answer key and front-matter cover pages are always printed regardless of this selection.
/// </summary>
/// <param name="IncludedSections">
/// The AYT sections to include. <c>null</c> means include every section (the default).
/// </param>
public sealed record PrintSelection(IReadOnlySet<DocumentSection>? IncludedSections = null)
{
    /// <summary>Include every section (default whole-document print).</summary>
    public static PrintSelection All { get; } = new();

    /// <summary>True when the given section should be printed under this selection.</summary>
    public bool Includes(DocumentSection section) =>
        IncludedSections is null || IncludedSections.Contains(section);
}
