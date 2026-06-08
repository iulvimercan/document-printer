using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocPrinter.Core.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Printing;

/// <summary>
/// Guided-flip duplex printer built on the SumatraPDF command line. It detects the document layout,
/// works out the exact pages and the two passes via <see cref="DuplexPlan"/>, prints the even
/// pass, awaits the caller's flip confirmation, then prints the odd pass. Each pass is a single
/// silent SumatraPDF invocation that prints at exact scale (no fit-to-page).
/// </summary>
public sealed class SumatraPdfPrinter(
    ISectionDetector sectionDetector,
    IOptions<PrintingOptions> options,
    ILogger<SumatraPdfPrinter> logger) : IDuplexPrinter
{
    private readonly PrintingOptions _options = options.Value;

    public async Task<PipelineResult> PrintDuplexAsync(
        DocumentFile file,
        Func<CancellationToken, Task<bool>> confirmFlip,
        PrintSelection? selection = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(confirmFlip);

        string sumatraPath = ResolveSumatraPath();

        DocumentLayout layout = await sectionDetector.DetectAsync(file, cancellationToken);
        DuplexPlan plan = DuplexPlan.Build(layout, selection ?? PrintSelection.All, _options.ReverseSecondPass);

        string target = string.IsNullOrWhiteSpace(_options.PrinterName) ? "the default printer" : _options.PrinterName;
        logger.LogInformation(
            "Printing {Total} page(s) of {Path} to {Target}: even pass {Even}, odd pass {Odd}",
            plan.Pages.Count, file.Path, target, plan.EvenPass.Count, plan.OddPass.Count);

        await PrintPassAsync(sumatraPath, file.Path, plan.EvenPass, cancellationToken);

        if (!await confirmFlip(cancellationToken))
        {
            return new PipelineResult(
                PipelineStage.PrintingEvenPages, Succeeded: false, file,
                "Even pages printed; flip was not confirmed, so the odd pass was aborted.");
        }

        await PrintPassAsync(sumatraPath, file.Path, plan.OddPass, cancellationToken);

        return new PipelineResult(
            PipelineStage.PrintingOddPages, Succeeded: true, file,
            $"Printed {plan.Pages.Count} page(s) to {target} in two passes.");
    }

    private async Task PrintPassAsync(
        string sumatraPath, string pdfPath, IReadOnlyList<int> pages, CancellationToken cancellationToken)
    {
        if (pages.Count == 0)
        {
            return;
        }

        string settings = string.Join(",", pages) + ",noscale";

        var startInfo = new ProcessStartInfo(sumatraPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (string.IsNullOrWhiteSpace(_options.PrinterName))
        {
            startInfo.ArgumentList.Add("-print-to-default");
        }
        else
        {
            startInfo.ArgumentList.Add("-print-to");
            startInfo.ArgumentList.Add(_options.PrinterName);
        }
        startInfo.ArgumentList.Add("-print-settings");
        startInfo.ArgumentList.Add(settings);
        startInfo.ArgumentList.Add("-silent");
        startInfo.ArgumentList.Add("-exit-when-done");
        startInfo.ArgumentList.Add(pdfPath);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start SumatraPDF at '{sumatraPath}'.");

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"SumatraPDF exited with code {process.ExitCode} while printing pages [{settings}].");
        }
    }

    private string ResolveSumatraPath()
    {
        string path = string.IsNullOrWhiteSpace(_options.SumatraPath)
            ? Path.Combine(AppContext.BaseDirectory, "tools", "SumatraPDF", "SumatraPDF.exe")
            : _options.SumatraPath;

        if (!File.Exists(path))
        {
            throw new SumatraPdfNotFoundException(path);
        }

        return path;
    }
}
