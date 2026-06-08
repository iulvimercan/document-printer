using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DocPrinter.Core.Models;
using DocPrinter.Printing;

namespace DocPrinter.Tests;

/// <summary>
/// Drives the real SumatraPDF printer against the cleaned 2025 TYT sample, targeting the
/// <c>Microsoft Print to PDF</c> virtual printer. A no-op unless <c>DOC_LIVE_TESTS=1</c> and the
/// sample is present. This is a guided/manual check: Windows pops a "Save Print Output As" dialog
/// once per pass, so save the even pass and the odd pass as separate files and interleave them to
/// confirm the reading order, the dropped instructions page, and front/back alignment.
/// Run with: <c>dotnet test --filter Category=Live</c>.
/// </summary>
[Trait("Category", "Live")]
public class SumatraPdfPrinterLiveTests
{
    [Fact]
    public async Task Prints_the_clean_tyt_sample_in_two_passes()
    {
        if (Environment.GetEnvironmentVariable("DOC_LIVE_TESTS") != "1")
        {
            return; // disabled by default
        }

        string sample = Path.Combine(RepoRoot(), "work", "samples", "yks-2025-tyt-clean.pdf");
        if (!File.Exists(sample))
        {
            return; // sample not produced on this machine
        }

        var options = Options.Create(new PrintingOptions { PrinterName = "Microsoft Print to PDF" });
        var detector = new SectionDetector(NullLogger<SectionDetector>.Instance);
        var printer = new SumatraPdfPrinter(detector, options, NullLogger<SumatraPdfPrinter>.Instance);

        PipelineResult result = await printer.PrintDuplexAsync(
            new DocumentFile(sample, new DocumentRequest(2025, DocumentSession.Standard)),
            confirmFlip: _ => Task.FromResult(true));

        Assert.True(result.Succeeded);
        Assert.Equal(PipelineStage.PrintingOddPages, result.Stage);
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
