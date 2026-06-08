using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace DocPrinter.Tests;

/// <summary>
/// Builds tiny real PDFs (Standard-14 Helvetica) that mimic an source exam's page structure, so the
/// section detector can be tested offline. Header lines are drawn in the top band of chosen pages;
/// every other page gets a single body line lower down. Returns the temp file path; the caller
/// deletes it. Text is kept ASCII (Helvetica has no Turkish glyphs) — the diacritic folding the
/// detector relies on is covered separately by TextNormalizerTests.
/// </summary>
internal static class SyntheticDocumentPdf
{
    // A4 in points. Top band (top 22%) starts at y ≈ 657, so header lines sit at 800/780.
    private const double Width = 595;
    private const double Height = 842;

    /// <summary>
    /// Builds a PDF of <paramref name="pages"/> pages. <paramref name="topLinesByPage"/> maps a
    /// 1-based page number to the header line(s) drawn at the top of that page.
    /// </summary>
    public static string Build(int pages, IReadOnlyDictionary<int, string[]> topLinesByPage)
    {
        var builder = new PdfDocumentBuilder();
        PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);

        for (int pageNumber = 1; pageNumber <= pages; pageNumber++)
        {
            PdfPageBuilder page = builder.AddPage(Width, Height);

            if (topLinesByPage.TryGetValue(pageNumber, out string[]? lines))
            {
                double y = 800;
                foreach (string line in lines)
                {
                    page.AddText(line, 14, new PdfPoint(60, y), font);
                    y -= 20; // next header line, still inside the top band
                }
            }

            // A bit of body text well below the top band so pages are never empty.
            page.AddText($"page {pageNumber} body", 11, new PdfPoint(60, 400), font);
        }

        string path = Path.Combine(Path.GetTempPath(), "yks-exam-" + Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, builder.Build());
        return path;
    }

    /// <summary>
    /// A 12-page AYT-like exam: covers on 1–2, the four section headers starting on 3/6/8/10,
    /// an instructions page on 11 and the answer key on 12. Section 2's header is split over two
    /// lines to exercise top-band line joining.
    /// </summary>
    public static string Sectioned() => Build(12, new Dictionary<int, string[]>
    {
        [3] = new[] { "TURK DILI ve EDEBIYATI-SOSYAL BILIMLER-1 TESTI" },
        [6] = new[] { "SOSYAL BILIMLER-2", "TESTI" },
        [8] = new[] { "MATEMATIK TESTI" },
        [10] = new[] { "FEN BILIMLERI TESTI" },
        [11] = new[] { "Sinav kurallari ve aciklamalar" },
        [12] = new[] { "CEVAP ANAHTARI" },
    });

    /// <summary>A 6-page TYT-like exam carrying TYT section headers that must be ignored.</summary>
    public static string Standard() => Build(6, new Dictionary<int, string[]>
    {
        [1] = new[] { "TURKCE TESTI" },
        [3] = new[] { "TEMEL MATEMATIK TESTI" },
        [5] = new[] { "FEN BILIMLERI TESTI" },
    });
}
