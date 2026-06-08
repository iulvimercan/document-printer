using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace DocPrinter.Downloader;

/// <summary>
/// Extracts navigable links from a rendered source page. Pure and browser-free: production passes
/// the live DOM (<c>await page.ContentAsync()</c>), tests pass saved HTML fixtures — both go
/// through the same code path. Relative URLs are resolved against <c>baseUrl</c>.
/// </summary>
public static class WebPageParser
{
    /// <summary>
    /// Returns every anchor on the page that has a non-empty href, with text whitespace-collapsed
    /// and the href resolved to an absolute URL. Used for the document-group and PDF hops.
    /// </summary>
    public static async Task<IReadOnlyList<CandidateLink>> ParseLinksAsync(string html, string baseUrl)
    {
        using IDocument document = await ParseAsync(html, baseUrl);

        List<CandidateLink> links = [];
        foreach (IHtmlAnchorElement anchor in document.QuerySelectorAll("a").OfType<IHtmlAnchorElement>())
        {
            string href = anchor.Href;
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            links.Add(new CandidateLink(CollapseWhitespace(anchor.TextContent), href));
        }

        return links;
    }

    /// <summary>
    /// Returns the year entries from the year selector. source renders these as
    /// <c>&lt;option value=/TR,&lt;id&gt;/&lt;year&gt;.html&gt;&lt;year&gt;&lt;/option&gt;</c>,
    /// so each option becomes a <see cref="CandidateLink"/> (text = year, href = absolute page URL).
    /// </summary>
    public static async Task<IReadOnlyList<CandidateLink>> ParseYearOptionsAsync(string html, string baseUrl)
    {
        using IDocument document = await ParseAsync(html, baseUrl);
        Uri? root = Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? b) ? b : null;

        List<CandidateLink> options = [];
        foreach (IHtmlOptionElement option in document.QuerySelectorAll("option").OfType<IHtmlOptionElement>())
        {
            string value = option.Value?.Trim() ?? string.Empty;
            string text = CollapseWhitespace(option.Text);
            if (value.Length == 0 || !text.All(char.IsDigit))
            {
                continue;
            }

            options.Add(new CandidateLink(text, Resolve(root, value)));
        }

        return options;
    }

    private static Task<IDocument> ParseAsync(string html, string baseUrl) =>
        BrowsingContext.New(Configuration.Default).OpenAsync(req => req.Content(html).Address(baseUrl));

    private static string Resolve(Uri? root, string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? absolute))
        {
            return absolute.AbsoluteUri;
        }

        return root is not null && Uri.TryCreate(root, value, out Uri? combined)
            ? combined.AbsoluteUri
            : value;
    }

    private static string CollapseWhitespace(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
