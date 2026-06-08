namespace DocPrinter.Core.Models;

/// <summary>
/// A contiguous, 1-based page range belonging to one AYT section, as found by the
/// section detector. <paramref name="StartPage"/> is the page whose top carries the section
/// header; <paramref name="EndPage"/> is the page before the next section starts.
/// </summary>
/// <param name="Section">Which AYT section this range covers.</param>
/// <param name="StartPage">First page of the section (1-based, inclusive).</param>
/// <param name="EndPage">Last page of the section (1-based, inclusive).</param>
public sealed record DocumentSectionRange(DocumentSection Section, int StartPage, int EndPage);
