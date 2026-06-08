namespace DocPrinter.Core.Models;

/// <summary>
/// A user's request for a specific past document, as understood by the agent.
/// The downloader uses these fields to locate the right file on the source site.
/// </summary>
/// <param name="Year">Document year, e.g. 2023.</param>
/// <param name="Session">Which session of the document (TYT, AYT, YDT).</param>
/// <param name="Booklet">Optional booklet/variant identifier if the document publishes more than one.</param>
public sealed record DocumentRequest(int Year, DocumentSession Session, string? Booklet = null);
