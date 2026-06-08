namespace DocPrinter.Downloader;

/// <summary>
/// A hyperlink discovered on an source page: its visible text and absolute href. These are the
/// candidates the <see cref="DocumentLinkSelector"/> chooses from at each navigation hop.
/// </summary>
/// <param name="Text">The link's trimmed visible text.</param>
/// <param name="Href">The link's absolute URL.</param>
public sealed record CandidateLink(string Text, string Href);
