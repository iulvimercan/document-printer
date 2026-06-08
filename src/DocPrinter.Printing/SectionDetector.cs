using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using DocPrinter.Core.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Printing;

/// <summary>
/// Finds the AYT section tests by their top-of-page headers (TYT prints whole, so it is not
/// scanned). For each AYT page it folds the words sitting in the top band of the page and matches
/// the distinctive token of each section header; the first page a token appears on is that
/// section's start. Ranges run from one section's start to the page before the next. The answer
/// key is the last page and the (optional) instructions page the one before it.
/// </summary>
public sealed class SectionDetector(ILogger<SectionDetector> logger) : ISectionDetector
{
    // Words whose vertical centre sits in the top 10% of the page are treated as header text.
    private const double TopBandFraction = 0.10;

    // Distinctive folded tokens per section (see TextNormalizer.Fold). Order is booklet order.
    private static readonly (DocumentSection Section, string Token)[] HeaderTokens =
    {
        (DocumentSection.LiteratureAndSocialSciences1, "EDEBIYAT"),
        (DocumentSection.SocialSciences2, "BILIMLER2"),
        (DocumentSection.Mathematics, "MATEMATIK"),
        (DocumentSection.Science, "FENBILIMLERI"),
    };

    public Task<DocumentLayout> DetectAsync(DocumentFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        cancellationToken.ThrowIfCancellationRequested();

        using PdfDocument document = PdfDocument.Open(file.Path);
        int pageCount = document.NumberOfPages;
        int answerKeyPage = pageCount;
        int instructionPage = pageCount - 1;

        IReadOnlyList<DocumentSectionRange> sections = file.Request.Session == DocumentSession.Sectioned
            ? DetectSections(document, instructionPage, cancellationToken)
            : Array.Empty<DocumentSectionRange>();

        logger.LogInformation(
            "Detected layout for {Path}: {Pages} pages, {Sections} AYT section(s)",
            file.Path, pageCount, sections.Count);

        return Task.FromResult(new DocumentLayout(pageCount, answerKeyPage, instructionPage, sections));
    }

    private IReadOnlyList<DocumentSectionRange> DetectSections(
        PdfDocument document, int instructionPage, CancellationToken cancellationToken)
    {
        // First page each section header appears on.
        var starts = new Dictionary<DocumentSection, int>();

        for (int pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string topText = FoldTopBand(document.GetPage(pageNumber));
            foreach ((DocumentSection section, string token) in HeaderTokens)
            {
                if (!starts.ContainsKey(section) && topText.Contains(token, StringComparison.Ordinal))
                {
                    starts[section] = pageNumber;
                }
            }
        }

        // Build contiguous ranges in booklet order; each ends the page before the next starts,
        // and the final one ends just before the instructions page.
        var ordered = starts
            .OrderBy(pair => pair.Value)
            .ToArray();

        var ranges = new List<DocumentSectionRange>(ordered.Length);
        for (int i = 0; i < ordered.Length; i++)
        {
            int start = ordered[i].Value;
            int end = (i + 1 < ordered.Length ? ordered[i + 1].Value - 1 : instructionPage - 1);
            ranges.Add(new DocumentSectionRange(ordered[i].Key, start, Math.Max(end, start)));
        }

        return ranges;
    }

    private static string FoldTopBand(Page page)
    {
        double threshold = page.Height * (1.0 - TopBandFraction);
        IEnumerable<Word> headerWords = page.GetWords()
            .Where(word => Centre(word.BoundingBox) >= threshold);

        return TextNormalizer.Fold(string.Concat(headerWords.Select(word => word.Text)));
    }

    private static double Centre(UglyToad.PdfPig.Core.PdfRectangle box) => (box.Top + box.Bottom) / 2.0;
}
