using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DocPrinter.Agent;
using DocPrinter.Agent.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Tests;

public class DocumentPipelineProgressTests
{
    [Fact]
    public async Task Prepare_reports_download_watermark_then_layout_in_order()
    {
        var detector = new FakeSectionDetector(
            new DocumentLayout(10, 10, 9, Array.Empty<DocumentSectionRange>()));
        string workingDirectory = Path.Combine(
            Path.GetTempPath(), "yks-tests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new AgentOptions { WorkingDirectory = workingDirectory });
        var pipeline = new DocumentPipeline(
            new FakeDocumentSource(), new FakeWatermarkRemover(), detector, new FakeDuplexPrinter(),
            options, NullLogger<DocumentPipeline>.Instance);
        var progress = new CapturingProgress();

        await pipeline.PrepareAsync(new DocumentRequest(2025, DocumentSession.Standard), progress);

        Assert.Equal(
            new[]
            {
                PipelineActivity.Downloading,
                PipelineActivity.RemovingWatermark,
                PipelineActivity.DetectingLayout,
            },
            progress.Reports);
    }

    [Fact]
    public async Task Prepare_without_progress_still_runs()
    {
        var detector = new FakeSectionDetector(
            new DocumentLayout(2, 2, 1, Array.Empty<DocumentSectionRange>()));
        string workingDirectory = Path.Combine(
            Path.GetTempPath(), "yks-tests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new AgentOptions { WorkingDirectory = workingDirectory });
        var pipeline = new DocumentPipeline(
            new FakeDocumentSource(), new FakeWatermarkRemover(), detector, new FakeDuplexPrinter(),
            options, NullLogger<DocumentPipeline>.Instance);

        CleanedDocument cleaned = await pipeline.PrepareAsync(new DocumentRequest(2025, DocumentSession.Standard));

        Assert.True(cleaned.File.IsWatermarkRemoved);
    }
}
