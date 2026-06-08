using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DocPrinter.Agent;
using DocPrinter.Agent.Abstractions;
using DocPrinter.Core.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Tests;

public class DocumentPrinterPluginTests
{
    private static DocumentLayout SectionedLayout() => new(12, 12, 11, new[]
    {
        new DocumentSectionRange(DocumentSection.LiteratureAndSocialSciences1, 3, 5),
        new DocumentSectionRange(DocumentSection.SocialSciences2, 6, 7),
        new DocumentSectionRange(DocumentSection.Mathematics, 8, 9),
        new DocumentSectionRange(DocumentSection.Science, 10, 10),
    });

    private static DocumentLayout StandardLayout() => new(43, 43, 42, Array.Empty<DocumentSectionRange>());

    [Fact]
    public async Task PrepareDocument_lists_sections_for_ayt()
    {
        (DocumentPrinterPlugin plugin, _) = BuildPlugin(SectionedLayout());

        string reply = await plugin.PrepareDocument(2024, "AYT");

        Assert.Contains("Matematik", reply);
        Assert.Contains("Fen Bilimleri", reply);
        Assert.Contains("12", reply); // page count surfaced
    }

    [Fact]
    public async Task PrepareDocument_notes_no_selection_for_tyt()
    {
        (DocumentPrinterPlugin plugin, _) = BuildPlugin(StandardLayout());

        string reply = await plugin.PrepareDocument(2025, "tyt");

        Assert.Contains("whole document", reply);
    }

    [Fact]
    public async Task PrepareDocument_rejects_ydt_without_running_the_pipeline()
    {
        (DocumentPrinterPlugin plugin, FakeDuplexPrinter printer) = BuildPlugin(StandardLayout());

        string reply = await plugin.PrepareDocument(2025, "YDT");

        Assert.Contains("YDT", reply);
        Assert.Null(printer.LastFile); // never reached printing
    }

    [Fact]
    public async Task PrintDocument_before_prepare_returns_a_friendly_error()
    {
        (DocumentPrinterPlugin plugin, FakeDuplexPrinter printer) = BuildPlugin(SectionedLayout());

        string reply = await plugin.PrintDocument("all");

        Assert.Contains("PrepareDocument first", reply);
        Assert.Null(printer.LastSelection);
    }

    [Fact]
    public async Task PrintDocument_parses_the_requested_sections_and_prints()
    {
        (DocumentPrinterPlugin plugin, FakeDuplexPrinter printer) = BuildPlugin(SectionedLayout());
        await plugin.PrepareDocument(2024, "AYT");

        string reply = await plugin.PrintDocument("matematik, fen");

        Assert.NotNull(printer.LastSelection);
        Assert.True(printer.LastSelection!.Includes(DocumentSection.Mathematics));
        Assert.True(printer.LastSelection.Includes(DocumentSection.Science));
        Assert.False(printer.LastSelection.Includes(DocumentSection.SocialSciences2));
        Assert.Contains("complete", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reports_ready_then_print_stages_through_progress()
    {
        var progress = new CapturingProgress();
        DocumentPrinterPlugin plugin = BuildPlugin(StandardLayout(), progress);

        await plugin.PrepareDocument(2025, "tyt");
        await plugin.PrintDocument();

        // The pipeline reports its prepare sub-steps; the plugin reports Ready and the print stages.
        // (AwaitingFlip/PrintingOddPages come from the flip prompt, not exercised by FakeFlipPrompt.)
        Assert.Equal(
            new[]
            {
                PipelineActivity.Downloading,
                PipelineActivity.RemovingWatermark,
                PipelineActivity.DetectingLayout,
                PipelineActivity.Ready,
                PipelineActivity.PrintingEvenPages,
                PipelineActivity.Completed,
            },
            progress.Reports);
    }

    [Fact]
    public async Task Reports_failed_when_prepare_throws()
    {
        var progress = new CapturingProgress();
        DocumentPrinterPlugin plugin = BuildPlugin(StandardLayout(), progress, new ThrowingDocumentSource());

        await plugin.PrepareDocument(2025, "tyt");

        // Download is reported before the source throws; the plugin then reports Failed.
        Assert.Equal(
            new[] { PipelineActivity.Downloading, PipelineActivity.Failed },
            progress.Reports);
    }

    private static (DocumentPrinterPlugin Plugin, FakeDuplexPrinter Printer) BuildPlugin(DocumentLayout layout)
    {
        var printer = new FakeDuplexPrinter();
        var plugin = BuildPlugin(layout, progress: null, printer: printer);
        return (plugin, printer);
    }

    private static DocumentPrinterPlugin BuildPlugin(
        DocumentLayout layout,
        IProgress<PipelineActivity>? progress,
        IDocumentSource? source = null,
        FakeDuplexPrinter? printer = null)
    {
        string workDir = Path.Combine(Path.GetTempPath(), "yks-tests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new AgentOptions { WorkingDirectory = workDir });
        var pipeline = new DocumentPipeline(
            source ?? new FakeDocumentSource(), new FakeWatermarkRemover(), new FakeSectionDetector(layout),
            printer ?? new FakeDuplexPrinter(), options, NullLogger<DocumentPipeline>.Instance);
        return new DocumentPrinterPlugin(
            pipeline, new FakeFlipPrompt(), NullLogger<DocumentPrinterPlugin>.Instance, progress);
    }
}
