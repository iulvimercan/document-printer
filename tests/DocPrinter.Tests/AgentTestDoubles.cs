using DocPrinter.Agent.Abstractions;
using DocPrinter.Core.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Tests;

/// <summary>Capturing fakes for the four Core services + the flip prompt, shared by the agent tests.</summary>
internal sealed class FakeDocumentSource : IDocumentSource
{
    public DocumentRequest? LastRequest { get; private set; }
    public string? LastDestination { get; private set; }

    public Task<DocumentFile> DownloadAsync(
        DocumentRequest request, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        LastDestination = Path.GetFullPath(destinationDirectory);
        string path = Path.Combine(LastDestination, "yks-downloaded.pdf");
        return Task.FromResult(new DocumentFile(path, request, "https://example/exam.pdf"));
    }
}

internal sealed class ThrowingDocumentSource : IDocumentSource
{
    public Task<DocumentFile> DownloadAsync(
        DocumentRequest request, string destinationDirectory, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("download failed");
}

internal sealed class FakeWatermarkRemover : IWatermarkRemover
{
    public string? LastDestination { get; private set; }

    public Task<DocumentFile> RemoveAsync(
        DocumentFile source, string destinationPath, CancellationToken cancellationToken = default)
    {
        LastDestination = destinationPath;
        return Task.FromResult(source with { Path = destinationPath, IsWatermarkRemoved = true });
    }
}

internal sealed class FakeSectionDetector(DocumentLayout layout) : ISectionDetector
{
    public DocumentLayout Layout { get; } = layout;

    public Task<DocumentLayout> DetectAsync(DocumentFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult(Layout);
}

internal sealed class FakeDuplexPrinter : IDuplexPrinter
{
    public PipelineResult Result { get; set; } = new(PipelineStage.PrintingOddPages, Succeeded: true);

    public DocumentFile? LastFile { get; private set; }
    public PrintSelection? LastSelection { get; private set; }
    public Func<CancellationToken, Task<bool>>? LastConfirmFlip { get; private set; }

    public Task<PipelineResult> PrintDuplexAsync(
        DocumentFile file, Func<CancellationToken, Task<bool>> confirmFlip,
        PrintSelection? selection = null, CancellationToken cancellationToken = default)
    {
        LastFile = file;
        LastSelection = selection;
        LastConfirmFlip = confirmFlip;
        return Task.FromResult(Result);
    }
}

internal sealed class FakeFlipPrompt(bool proceed = true) : IFlipPrompt
{
    public Task<bool> ConfirmFlipAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(proceed);
}

internal sealed class CapturingProgress : IProgress<PipelineActivity>
{
    public List<PipelineActivity> Reports { get; } = new();

    public void Report(PipelineActivity value) => Reports.Add(value);
}
