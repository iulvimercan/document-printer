namespace DocPrinter.Core.Models;

/// <summary>
/// The page structure of a (cleaned) document PDF, as discovered by the section detector. Drives
/// what gets printed: which pages belong to which AYT section, and where the always-printed
/// answer key and the optional trailing instructions page sit.
/// </summary>
/// <param name="PageCount">Total number of pages in the PDF.</param>
/// <param name="AnswerKeyPage">The 1-based last page — the answer key, always printed.</param>
/// <param name="InstructionPage">
/// The 1-based second-to-last page — an unnecessary instructions page, printed only when keeping
/// it yields an even total (a parity filler for clean duplex).
/// </param>
/// <param name="Sections">
/// The AYT section ranges, in order. Empty for TYT (no per-section selection — print everything).
/// </param>
public sealed record DocumentLayout(
    int PageCount,
    int AnswerKeyPage,
    int InstructionPage,
    IReadOnlyList<DocumentSectionRange> Sections);
