using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using DocPrinter.Core.Models;
using DocPrinter.Watermark;

namespace DocPrinter.Tests;

/// <summary>
/// Exercises the remover against the real source sample saved in <c>work/samples</c>. The sample
/// is git-ignored, so this is a no-op unless <c>DOC_LIVE_TESTS=1</c> and the file is present.
/// Run with: <c>dotnet test --filter Category=Live</c>.
/// </summary>
[Trait("Category", "Live")]
public class PdfWatermarkRemoverLiveTests
{
    [Fact]
    public async Task Removes_watermark_from_the_real_2025_tyt_sample()
    {
        if (Environment.GetEnvironmentVariable("DOC_LIVE_TESTS") != "1")
        {
            return; // disabled by default
        }

        string sample = Path.Combine(RepoRoot(), "work", "samples", "yks-2025-tyt.pdf");
        if (!File.Exists(sample))
        {
            return; // sample not downloaded on this machine
        }

        int inputPages;
        using (PdfDocument input = PdfReader.Open(sample, PdfDocumentOpenMode.Modify))
        {
            inputPages = input.PageCount;
        }

        string destination = Path.Combine(
            Path.GetTempPath(), "yks-2025-tyt-clean-" + Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            var remover = new PdfWatermarkRemover(NullLogger<PdfWatermarkRemover>.Instance);

            DocumentFile result = await remover.RemoveAsync(
                new DocumentFile(sample, new DocumentRequest(2025, DocumentSession.Standard)), destination);

            Assert.True(result.IsWatermarkRemoved);
            Assert.True(File.Exists(destination));

            using PdfDocument output = PdfReader.Open(destination, PdfDocumentOpenMode.Modify);
            Assert.Equal(inputPages, output.PageCount); // content preserved, count unchanged
            // Any watermark form still present (e.g. the tagged artwork) must be emptied.
            Assert.All(WatermarkDetector.Find(output), f => Assert.Empty(f.Stream!.UnfilteredValue));
        }
        finally
        {
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }
        }
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DocPrinter.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? AppContext.BaseDirectory;
    }
}
