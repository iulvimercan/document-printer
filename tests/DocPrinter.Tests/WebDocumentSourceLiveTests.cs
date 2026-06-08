using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DocPrinter.Core.Models;
using DocPrinter.Downloader;

namespace DocPrinter.Tests;

/// <summary>
/// Exercises the real Playwright + source path. Network- and browser-dependent, so it is a no-op
/// unless <c>DOC_LIVE_TESTS=1</c>. Run with: <c>dotnet test --filter Category=Live</c>.
/// Requires the Playwright Chromium browser to be installed.
/// </summary>
[Trait("Category", "Live")]
public class WebDocumentSourceLiveTests
{
    [Fact]
    public async Task Downloads_2025_TYT_pdf_from_osym()
    {
        if (Environment.GetEnvironmentVariable("DOC_LIVE_TESTS") != "1")
        {
            return; // disabled by default
        }

        var source = new WebDocumentSource(
            Options.Create(new DownloaderOptions()),
            NullLogger<WebDocumentSource>.Instance);

        string dir = Path.Combine(Path.GetTempPath(), "yks-live-" + Guid.NewGuid().ToString("N"));
        try
        {
            DocumentFile file = await source.DownloadAsync(new DocumentRequest(2025, DocumentSession.Standard), dir);

            Assert.True(File.Exists(file.Path));
            Assert.Equal("https://dokuman.osym.gov.tr/pdfdokuman/2025/YKS/TSK/yks_tyt_2025_kitapcik_d250.pdf", file.SourceUrl);
            Assert.False(file.IsWatermarkRemoved);

            byte[] head = new byte[5];
            await using (FileStream fs = File.OpenRead(file.Path))
            {
                Assert.Equal(5, await fs.ReadAsync(head));
            }

            Assert.Equal("%PDF-", Encoding.ASCII.GetString(head));
            Assert.True(new FileInfo(file.Path).Length > 100_000, "a real exam booklet PDF should be large");
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
