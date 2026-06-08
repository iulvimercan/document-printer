using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using DocPrinter.Core.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Downloader;

/// <summary>
/// Downloads past document PDFs from the official source site by driving a headless browser.
/// The browser only performs deterministic navigation (the three hops year → document-group → PDF);
/// <see cref="DocumentLinkSelector"/> decides which link to follow. The PDF itself lives on a
/// separate document host (configured via <see cref="DownloaderOptions.DocumentHost"/>), which
/// serves it directly to a plain HTTP GET with a browser User-Agent — so the bytes are fetched
/// with <see cref="HttpClient"/> rather than the browser.
/// </summary>
public sealed class WebDocumentSource(IOptions<DownloaderOptions> options, ILogger<WebDocumentSource> logger)
    : IDocumentSource
{
    private const string DesktopUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";

    private static readonly HttpClient Http = CreateHttpClient();

    private readonly DownloaderOptions _options = options.Value;

    /// <summary>
    /// Ensures Playwright's Chromium is installed, running the install at most once per process.
    /// This is the same call <c>playwright.ps1 install chromium</c> makes, so it is idempotent (a
    /// fast no-op when the browser is already present) and self-healing on a fresh machine. Running
    /// it in-process means it installs into exactly the environment the browser is launched from.
    /// <see cref="Lazy{T}"/> is thread-safe by default, so concurrent Web circuits install once.
    /// </summary>
    private readonly Lazy<int> _browserInstall = new(() =>
    {
        if (!options.Value.AutoInstallBrowser)
        {
            return 0;
        }

        logger.LogInformation("Ensuring Playwright Chromium is installed…");
        return Microsoft.Playwright.Program.Main(["install", "chromium"]);
    });

    public async Task<DocumentFile> DownloadAsync(
        DocumentRequest request,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        DocumentLinkSelector.EnsureSupported(request);

        string destDir = Path.GetFullPath(destinationDirectory);
        Directory.CreateDirectory(destDir);

        // First-ever call may download Chromium (kept off the request's sync path); later calls are
        // an instant no-op. Throw with the manual fallback if the install reports failure.
        int installExit = await Task.Run(() => _browserInstall.Value, cancellationToken);
        if (installExit != 0)
        {
            throw new InvalidOperationException(
                $"Playwright Chromium install failed (exit code {installExit}). " +
                "Run 'playwright.ps1 install chromium' manually.");
        }

        using IPlaywright playwright = await Playwright.CreateAsync();
        await using IBrowser browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = _options.Headless,
        });
        await using IBrowserContext context = await browser.NewContextAsync(new()
        {
            UserAgent = DesktopUserAgent,
        });
        IPage page = await context.NewPageAsync();
        page.SetDefaultNavigationTimeout(_options.NavigationTimeoutMs);

        logger.LogInformation("Opening source entry page {EntryUrl}", _options.EntryUrl);
        await page.GotoAsync(_options.EntryUrl);
        var yearOptions = await WebPageParser.ParseYearOptionsAsync(await page.ContentAsync(), page.Url);
        var yearLink = DocumentLinkSelector.SelectYear(yearOptions, request.Year);

        logger.LogInformation("Navigating to year {Year}: {Href}", request.Year, yearLink.Href);
        var groupLink = DocumentLinkSelector.SelectDocumentGroup(
            await LinksAfterNavigateAsync(page, yearLink.Href), request.Year);

        logger.LogInformation("Opening document group: {Href}", groupLink.Href);
        var pdfLink = DocumentLinkSelector.SelectDocumentPdf(
            await LinksAfterNavigateAsync(page, groupLink.Href), request, _options.DocumentHost);

        logger.LogInformation("Found {Session} {Year} PDF: {Href}",
            request.Session, request.Year, pdfLink.Href);

        cancellationToken.ThrowIfCancellationRequested();

        string destinationPath = Path.Combine(destDir, BuildFileName(request));
        await DownloadPdfAsync(pdfLink.Href, groupLink.Href, destinationPath, cancellationToken);

        logger.LogInformation("Saved document to {Path}", destinationPath);
        return new DocumentFile(destinationPath, request, pdfLink.Href, IsWatermarkRemoved: false);
    }

    private static async Task<IReadOnlyList<CandidateLink>> LinksAfterNavigateAsync(IPage page, string url)
    {
        await page.GotoAsync(url);
        string html = await page.ContentAsync();
        return await WebPageParser.ParseLinksAsync(html, page.Url);
    }

    private static async Task DownloadPdfAsync(
        string pdfUrl, string referer, string destinationPath, CancellationToken ct)
    {
        // One retry: the source document host may answer the first hit with a cookie-setting
        // challenge page; the retry carries that cookie and gets the real file.
        byte[] body = await GetBytesAsync(pdfUrl, referer, ct);
        if (!LooksLikePdf(body))
        {
            body = await GetBytesAsync(pdfUrl, referer, ct);
        }

        if (!LooksLikePdf(body))
        {
            throw new InvalidOperationException(
                $"Expected a PDF from {pdfUrl} but received {body.Length} bytes that are not a PDF.");
        }

        await File.WriteAllBytesAsync(destinationPath, body, ct);
    }

    private static async Task<byte[]> GetBytesAsync(string url, string referer, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (Uri.TryCreate(referer, UriKind.Absolute, out Uri? refererUri))
        {
            request.Headers.Referrer = refererUri;
        }

        using HttpResponseMessage response = await Http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private static bool LooksLikePdf(byte[] bytes) =>
        bytes.Length >= 5 && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F' && bytes[4] == '-';

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            AllowAutoRedirect = true,
        };

        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(DesktopUserAgent);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/pdf,*/*");
        return client;
    }

    private static string BuildFileName(DocumentRequest request)
    {
        string session = request.Session.ToString().ToLowerInvariant();
        string booklet = request.Booklet is { Length: > 0 } b ? "-" + Sanitize(b) : string.Empty;
        return $"doc-{request.Year}-{session}{booklet}.pdf";
    }

    private static string Sanitize(string value)
    {
        char[] chars = value.Trim().ToLowerInvariant().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
            {
                chars[i] = '-';
            }
        }

        return new string(chars);
    }
}
