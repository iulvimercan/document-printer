using DocPrinter.Core.Models;

namespace DocPrinter.Printing;

/// <summary>
/// The pure, printer-independent decision of exactly which pages to print and how to split them
/// across the two manual-duplex passes. All page-selection, parity, and re-indexing rules live
/// here so they can be unit-tested without touching a real printer.
/// </summary>
/// <param name="Pages">The full set of pages to print, sorted ascending. Always an even count.</param>
/// <param name="EvenPass">
/// Pages printed in the first pass — the 1-indexed even positions of <paramref name="Pages"/>
/// (the "even pages" the charter prints before the flip).
/// </param>
/// <param name="OddPass">
/// Pages printed in the second pass after the flip — the 1-indexed odd positions of
/// <paramref name="Pages"/>, reversed when the printer requires it.
/// </param>
public sealed record DuplexPlan(
    IReadOnlyList<int> Pages,
    IReadOnlyList<int> EvenPass,
    IReadOnlyList<int> OddPass)
{
    /// <summary>
    /// Builds the plan from a detected <paramref name="layout"/> and a section
    /// <paramref name="selection"/>.
    /// </summary>
    /// <param name="layout">The detected page structure.</param>
    /// <param name="selection">Which AYT sections to keep (ignored when the layout has no sections).</param>
    /// <param name="reverseSecondPass">When true, the second (odd) pass is emitted in reverse order.</param>
    public static DuplexPlan Build(DocumentLayout layout, PrintSelection selection, bool reverseSecondPass)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(selection);

        var pages = new SortedSet<int>(Enumerable.Range(1, layout.PageCount));

        // Drop excluded AYT sections (TYT has no sections, so nothing is dropped).
        foreach (DocumentSectionRange section in layout.Sections)
        {
            if (selection.Includes(section.Section))
            {
                continue;
            }

            for (int page = section.StartPage; page <= section.EndPage; page++)
            {
                pages.Remove(page);
            }
        }

        // The trailing instructions page is unnecessary: drop it, then re-add it only if doing so
        // is what keeps the printed total even (clean two-sided sheets, no half-blank sheet).
        bool instructionIsValid = layout.InstructionPage >= 1 && layout.InstructionPage <= layout.PageCount;
        if (instructionIsValid)
        {
            pages.Remove(layout.InstructionPage);
            if (pages.Count % 2 != 0)
            {
                pages.Add(layout.InstructionPage);
            }
        }

        var ordered = pages.ToArray();

        // Re-index within the selection: consecutive selected pages form sheets. The first pass
        // prints the even positions (1-indexed), the second pass the odd positions.
        var oddPass = new List<int>();
        var evenPass = new List<int>();
        for (int i = 0; i < ordered.Length; i++)
        {
            (((i + 1) % 2 == 0) ? evenPass : oddPass).Add(ordered[i]);
        }

        if (reverseSecondPass)
        {
            oddPass.Reverse();
        }

        return new DuplexPlan(ordered, evenPass, oddPass);
    }
}
