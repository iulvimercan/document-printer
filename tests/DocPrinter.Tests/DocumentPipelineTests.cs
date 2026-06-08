using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DocPrinter.Agent;
using DocPrinter.Core.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Tests;

public class DocumentPipelineTests
{
    [Fact]
    public async Task Prepare_downloads_dewatermarks_then_detects_layout()
    {
        var source = new FakeDocumentSource();
        var remover = new FakeWatermarkRemover();
        var detector = new FakeSectionDetector(new DocumentLayout(12, 12, 11, Array.Empty<DocumentSectionRange>()));
        DocumentPipeline pipeline = Build(source, remover, detector, new FakeDuplexPrinter(), out string workDir);

        var request = new DocumentRequest(2025, DocumentSession.Standard);
        CleanedDocument cleaned = await pipeline.PrepareAsync(request);

        Assert.Equal(request, source.LastRequest);
        Assert.Equal(workDir, source.LastDestination);          // downloaded into the working dir
        Assert.True(cleaned.File.IsWatermarkRemoved);           // watermark removal ran
        Assert.Equal(remover.LastDestination, cleaned.File.Path);
        Assert.EndsWith("-clean.pdf", cleaned.File.Path);       // cleaned-file naming
        Assert.Same(detector.Layout, cleaned.Layout);           // detector output carried through
    }

    [Fact]
    public async Task Print_forwards_the_selection_and_flip_callback_and_returns_the_result()
    {
        var expected = new PipelineResult(PipelineStage.PrintingOddPages, Succeeded: true);
        var printer = new FakeDuplexPrinter { Result = expected };
        DocumentPipeline pipeline = Build(new FakeDocumentSource(), new FakeWatermarkRemover(),
            new FakeSectionDetector(new DocumentLayout(4, 4, 3, Array.Empty<DocumentSectionRange>())), printer, out _);

        var exam = new CleanedDocument(
            new DocumentFile("clean.pdf", new DocumentRequest(2024, DocumentSession.Sectioned), IsWatermarkRemoved: true),
            new DocumentLayout(4, 4, 3, Array.Empty<DocumentSectionRange>()));
        var selection = new PrintSelection(new HashSet<DocumentSection> { DocumentSection.Mathematics });
        Func<CancellationToken, Task<bool>> flip = _ => Task.FromResult(true);

        PipelineResult result = await pipeline.PrintAsync(exam, selection, flip);

        Assert.Same(expected, result);
        Assert.Same(selection, printer.LastSelection);
        Assert.Same(flip, printer.LastConfirmFlip);
        Assert.Same(exam.File, printer.LastFile);
    }

    [Fact]
    public async Task Print_preserves_a_declined_flip_result()
    {
        var declined = new PipelineResult(PipelineStage.PrintingEvenPages, Succeeded: false,
            Message: "flip not confirmed");
        var printer = new FakeDuplexPrinter { Result = declined };
        DocumentPipeline pipeline = Build(new FakeDocumentSource(), new FakeWatermarkRemover(),
            new FakeSectionDetector(new DocumentLayout(2, 2, 1, Array.Empty<DocumentSectionRange>())), printer, out _);

        var exam = new CleanedDocument(
            new DocumentFile("clean.pdf", new DocumentRequest(2024, DocumentSession.Standard)),
            new DocumentLayout(2, 2, 1, Array.Empty<DocumentSectionRange>()));

        PipelineResult result = await pipeline.PrintAsync(
            exam, PrintSelection.All, _ => Task.FromResult(false));

        Assert.False(result.Succeeded);
        Assert.Equal(PipelineStage.PrintingEvenPages, result.Stage);
    }

    private static DocumentPipeline Build(
        IDocumentSource source, IWatermarkRemover remover, ISectionDetector detector,
        IDuplexPrinter printer, out string workingDirectory)
    {
        workingDirectory = Path.Combine(Path.GetTempPath(), "yks-tests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new AgentOptions { WorkingDirectory = workingDirectory });
        return new DocumentPipeline(source, remover, detector, printer, options,
            NullLogger<DocumentPipeline>.Instance);
    }
}
