namespace DocPrinter.Downloader;

/// <summary>
/// Configuration for the source downloader. The entry URL and document host are deliberately not
/// hardcoded — supply them via User Secrets under the <c>"Downloader"</c> key so they are never
/// committed to the repository. The entry URL's landing-page id also changes year to year.
/// </summary>
public sealed class DownloaderOptions
{
    /// <summary>
    /// The page the downloader starts from. It must expose the year dropdown so the requested year
    /// can be reached. Supplied via User Secrets (<c>"Downloader:EntryUrl"</c>); empty by default.
    /// </summary>
    public string EntryUrl { get; set; } = "";

    /// <summary>
    /// Host substring used to recognize document download links (e.g. the CDN/document host).
    /// Supplied via User Secrets (<c>"Downloader:DocumentHost"</c>); empty by default, in which case
    /// candidate links are matched by their <c>.pdf</c> suffix only.
    /// </summary>
    public string DocumentHost { get; set; } = "";

    /// <summary>Run the browser without a visible window. Off only for debugging.</summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Install the Playwright Chromium browser on first use if it is missing. On by default so a
    /// fresh machine works out of the box without a separate <c>playwright.ps1 install</c> step;
    /// disable in tests/CI to keep them offline.
    /// </summary>
    public bool AutoInstallBrowser { get; set; } = true;

    /// <summary>Per-navigation timeout in milliseconds.</summary>
    public int NavigationTimeoutMs { get; set; } = 30_000;
}
