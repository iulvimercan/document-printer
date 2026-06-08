using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocPrinter.Agent.Abstractions;
using DocPrinter.Core.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Agent;

/// <summary>
/// The deterministic, LLM-free orchestrator behind the agent. It runs the real pipeline by
/// sequencing the four <c>Core</c> services, split into two phases so the agent can interleave a
/// user decision (which AYT sections) between them:
/// <list type="number">
/// <item><see cref="PrepareAsync"/> — download → remove watermark → detect layout.</item>
/// <item><see cref="PrintAsync"/> — guided-flip duplex print of the chosen selection.</item>
/// </list>
/// All the sequencing lives here, with no Semantic Kernel dependency, so it is fully unit-testable
/// against fakes of the Core interfaces.
/// </summary>
public sealed class DocumentPipeline(
    IDocumentSource source,
    IWatermarkRemover watermarkRemover,
    ISectionDetector sectionDetector,
    IDuplexPrinter printer,
    IOptions<AgentOptions> options,
    ILogger<DocumentPipeline> logger)
{
    private readonly AgentOptions _options = options.Value;

    /// <summary>
    /// Downloads the requested document, strips its watermark, and detects its page layout. Reports each
    /// sub-step through <paramref name="progress"/> so a UI can show live status.
    /// </summary>
    public async Task<CleanedDocument> PrepareAsync(
        DocumentRequest request,
        IProgress<PipelineActivity>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string workingDirectory = Path.GetFullPath(_options.WorkingDirectory);
        Directory.CreateDirectory(workingDirectory);

        progress?.Report(PipelineActivity.Downloading);
        logger.LogInformation("Downloading {Session} {Year}", request.Session, request.Year);
        DocumentFile downloaded = await source.DownloadAsync(request, workingDirectory, cancellationToken);

        string cleanedPath = BuildCleanedPath(downloaded.Path);
        progress?.Report(PipelineActivity.RemovingWatermark);
        logger.LogInformation("Removing watermark → {Path}", cleanedPath);
        DocumentFile cleaned = await watermarkRemover.RemoveAsync(downloaded, cleanedPath, cancellationToken);

        progress?.Report(PipelineActivity.DetectingLayout);
        logger.LogInformation("Detecting page layout");
        DocumentLayout layout = await sectionDetector.DetectAsync(cleaned, cancellationToken);

        return new CleanedDocument(cleaned, layout);
    }

    /// <summary>
    /// Prints a prepared document with the guided-flip workflow, printing only the chosen sections
    /// (the answer key and cover pages are always printed).
    /// </summary>
    public Task<PipelineResult> PrintAsync(
        CleanedDocument document,
        PrintSelection selection,
        Func<CancellationToken, Task<bool>> confirmFlip,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Printing {Path}", document.File.Path);
        return printer.PrintDuplexAsync(document.File, confirmFlip, selection, cancellationToken);
    }

    private static string BuildCleanedPath(string downloadedPath)
    {
        string directory = Path.GetDirectoryName(downloadedPath) ?? ".";
        string name = Path.GetFileNameWithoutExtension(downloadedPath);
        return Path.Combine(directory, $"{name}-clean.pdf");
    }
}
