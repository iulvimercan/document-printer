using DocPrinter.Core.Models;

namespace DocPrinter.Downloader;

/// <summary>
/// Chooses the correct link at each source navigation hop for a given <see cref="DocumentRequest"/>.
/// Pure and deterministic so it can be unit-tested offline — and so the Step 5 agent can later
/// supply its own choice without changing the Playwright driver. Matching is keyword/URL-shape
/// based to tolerate source's year-to-year layout drift.
/// </summary>
public static class DocumentLinkSelector
{
    /// <summary>
    /// Guards requests the downloader cannot yet serve. The 'YDT' session is out of scope for
    /// now. Call before launching the browser to fail fast.
    /// </summary>
    public static void EnsureSupported(DocumentRequest request)
    {
        if (request.Session == DocumentSession.Specialized)
        {
            throw new NotSupportedException(
                "The 'YDT' session is not supported yet. Request a TYT or AYT document instead.");
        }
    }

    /// <summary>Picks the year page from the year selector (e.g. the link for 2025).</summary>
    public static CandidateLink SelectYear(IReadOnlyList<CandidateLink> links, int year)
    {
        string yearText = year.ToString();
        List<CandidateLink> matches = links
            .Where(l => l.Text.Trim() == yearText || EndsWithYearPage(l.Href, year))
            .ToList();

        return Single(matches, $"year {year}");
    }

    /// <summary>Picks the document-group (intermediate) link on a year page.</summary>
    public static CandidateLink SelectDocumentGroup(IReadOnlyList<CandidateLink> links, int year)
    {
        string yearText = year.ToString();
        List<CandidateLink> matches = links
            .Where(l =>
            {
                string haystack = (l.Text + " " + l.Href).ToLowerInvariant();
                return haystack.Contains(yearText)
                    && haystack.Contains("yks")
                    && (haystack.Contains("kitap") || haystack.Contains("soru") || haystack.Contains("anahtar"));
            })
            .ToList();

        return Single(matches, $"document group for {year}");
    }

    /// <summary>Picks the PDF link for the requested session (TYT/AYT) on the document-group page.</summary>
    public static CandidateLink SelectDocumentPdf(
        IReadOnlyList<CandidateLink> links, DocumentRequest request, string? documentHost = null)
    {
        EnsureSupported(request);

        string token = request.Session switch
        {
            DocumentSession.Standard => "tyt",
            DocumentSession.Sectioned => "ayt",
            _ => throw new NotSupportedException($"Unhandled session {request.Session}."),
        };

        List<CandidateLink> pdfs = links.Where(l => IsPdf(l, documentHost)).ToList();
        List<CandidateLink> matches = pdfs.Where(l => MatchesSession(l, token)).ToList();

        if (request.Booklet is { Length: > 0 } booklet)
        {
            string needle = booklet.ToLowerInvariant();
            List<CandidateLink> narrowed = matches
                .Where(l => (l.Text + " " + l.Href).ToLowerInvariant().Contains(needle))
                .ToList();
            if (narrowed.Count > 0)
            {
                matches = narrowed;
            }
        }

        return Single(matches, $"{request.Session} PDF for {request.Year}");
    }

    private static bool IsPdf(CandidateLink link, string? documentHost) =>
        link.Href.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
        || (!string.IsNullOrEmpty(documentHost)
            && link.Href.Contains(documentHost, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesSession(CandidateLink link, string token)
    {
        string href = link.Href.ToLowerInvariant();
        string text = link.Text.ToLowerInvariant();
        // Prefer the stable filename token (e.g. "_tyt_"); fall back to the visible code in text.
        return href.Contains("_" + token + "_") || href.Contains("/" + token + "_")
            || ContainsWord(text, token);
    }

    private static bool ContainsWord(string text, string token)
    {
        int index = text.IndexOf(token, StringComparison.Ordinal);
        while (index >= 0)
        {
            bool leftOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            int after = index + token.Length;
            bool rightOk = after >= text.Length || !char.IsLetterOrDigit(text[after]);
            if (leftOk && rightOk)
            {
                return true;
            }

            index = text.IndexOf(token, index + 1, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool EndsWithYearPage(string href, int year) =>
        href.EndsWith($"/{year}.html", StringComparison.OrdinalIgnoreCase);

    private static CandidateLink Single(IReadOnlyList<CandidateLink> matches, string what) => matches.Count switch
    {
        1 => matches[0],
        0 => throw new InvalidOperationException($"No link found for {what} on the source page."),
        _ => throw new InvalidOperationException(
            $"Ambiguous link for {what}: {matches.Count} candidates matched. " +
            "Specify a booklet to disambiguate."),
    };
}
