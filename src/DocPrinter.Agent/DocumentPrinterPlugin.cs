using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using DocPrinter.Agent.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Agent;

/// <summary>
/// The Semantic Kernel plugin that exposes the pipeline to the LLM as two tools. It is a thin
/// bridge: the model decides <em>which</em> document and <em>which</em> sections; the deterministic
/// <see cref="DocumentPipeline"/> does the downloading, watermark removal, and printing. The plugin
/// holds the per-conversation state — the document prepared by <see cref="PrepareDocument"/> — so the model
/// can offer section choices before <see cref="PrintDocument"/> runs.
/// </summary>
public sealed class DocumentPrinterPlugin(
    DocumentPipeline pipeline,
    IFlipPrompt flipPrompt,
    ILogger<DocumentPrinterPlugin> logger,
    IProgress<PipelineActivity>? progress = null)
{
    private CleanedDocument? _prepared;

    [KernelFunction]
    [Description(
        "Download a document from the configured source, remove its watermark, and analyze its page " +
        "layout. Always call this before printing. 'session' must be TYT or AYT (YDT is not supported).")]
    public async Task<string> PrepareDocument(
        [Description("Document year, e.g. 2025")] int year,
        [Description("Document session: TYT or AYT")]
        string session,
        [Description("Optional booklet/variant code; omit if there is only one")]
        string? booklet = null,
        CancellationToken cancellationToken = default)
    {
        DocumentSession parsedSession;
        try
        {
            parsedSession = SessionParser.Parse(session);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return ex.Message;
        }

        var request = new DocumentRequest(year, parsedSession, booklet);
        try
        {
            _prepared = await pipeline.PrepareAsync(request, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            progress?.Report(PipelineActivity.Failed);
            logger.LogError(ex, "Failed to prepare {Session} {Year}", parsedSession, year);
            return $"Could not prepare the {parsedSession} {year} document: {ex.Message}";
        }

        progress?.Report(PipelineActivity.Ready);
        return Describe(parsedSession, year, _prepared);
    }

    [KernelFunction]
    [Description(
        "Print the prepared document double-sided using the guided manual-flip workflow. The optional " +
        "'sections' argument limits which sections print (e.g. 'matematik, fen'); omit it or " +
        "pass 'all' to print everything. It is ignored for TYT. Call PrepareDocument first.")]
    public async Task<string> PrintDocument(
        [Description("Sections to print, comma-separated; omit or 'all' for the whole document")]
        string? sections = null,
        CancellationToken cancellationToken = default)
    {
        if (_prepared is null)
        {
            return "No document is prepared yet. Call PrepareDocument first.";
        }

        if (sections is not null)
        {
            logger.LogInformation("Sections to print: {0}",  sections);
        }

        PrintSelection selection;
        try
        {
            selection = SectionParser.Parse(sections);
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }

        progress?.Report(PipelineActivity.PrintingEvenPages);
        PipelineResult result = await pipeline.PrintAsync(
            _prepared, selection, flipPrompt.ConfirmFlipAsync, cancellationToken);
        progress?.Report(result.Succeeded ? PipelineActivity.Completed : PipelineActivity.Failed);

        if (result.Message is { Length: > 0 } message)
        {
            return message;
        }

        return result.Succeeded
            ? "Printing complete."
            : "Printing did not finish (the flip was not confirmed).";
    }

    private static string Describe(DocumentSession session, int year, CleanedDocument document)
    {
        DocumentLayout layout = document.Layout;
        if (session != DocumentSession.Sectioned || layout.Sections.Count == 0)
        {
            return $"The {session} {year} document is ready ({layout.PageCount} pages). " +
                   "It has no separately selectable sections, so the whole document will be printed. " +
                   "Ask whether to start printing.";
        }

        IEnumerable<string> names = layout.Sections.Select(s => SectionName(s.Section));
        return $"The {year} document is ready ({layout.PageCount} pages). " +
               $"Its sections are: {string.Join(", ", names)}. " +
               "Unless the user has given you sections and told you to " +
               "remember it. Display the list of the sections with numbers starting from 1. " +
               "Ask which sections to print (or all of them), then print. The user can give " +
               "the answer as numerical order too.";
    }

    private static string SectionName(DocumentSection section) => section switch
    {
        DocumentSection.LiteratureAndSocialSciences1 => "Türk Dili ve Edebiyatı / Sosyal Bilimler-1",
        DocumentSection.SocialSciences2 => "Sosyal Bilimler-2",
        DocumentSection.Mathematics => "Matematik",
        DocumentSection.Science => "Fen Bilimleri",
        _ => section.ToString(),
    };
}