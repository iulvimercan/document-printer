using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;

namespace DocPrinter.Tests;

/// <summary>
/// Builds tiny throwaway PDFs that mimic the source watermark structure, so the remover can be
/// tested offline (the real 3.4 MB sample is git-ignored and only used by the gated live test).
/// Each builder returns the path of a temp file the caller is responsible for deleting.
/// </summary>
internal static class SyntheticPdf
{
    // a, b, c, d, e, f for a cm matrix. b/c non-zero ⇒ rotation. Translation kept at 0.
    public const string Rotation45 = "0.7071 0.7071 -0.7071 0.7071 0 0";
    public const string Rotation30 = "0.8660 0.5 -0.5 0.8660 0 0";

    /// <summary>Pages paint a diagonal overlay form (no Adobe tag) — exercises the geometry detector.</summary>
    public static string DiagonalOverlay(int pages, string matrix) =>
        Build(pages, form => SetContent(form, $"q {matrix} cm /Fm0 Do Q"));

    /// <summary>A form tagged /PieceInfo → /ADBE_CompoundType → /Private /Watermark — exercises the tag detector.</summary>
    public static string TaggedWatermark(int pages) =>
        Build(pages, form =>
        {
            Tag(form);
            SetContent(form, "BT /T1 12 Tf (sample) Tj ET");
        });

    /// <summary>Ordinary upright content, no watermark form at all.</summary>
    public static string WithoutWatermark(int pages) => Build(pages, configureForm: null);

    private static string Build(int pages, Action<PdfDictionary>? configureForm)
    {
        using var document = new PdfDocument();

        PdfDictionary? watermark = configureForm is null ? null : NewForm(document, configureForm);

        for (int i = 0; i < pages; i++)
        {
            PdfPage page = document.AddPage();
            if (watermark is null)
            {
                continue;
            }

            // Reference the watermark form from the page's resources so it survives Save and is
            // reachable by the detector's object scan. (Painting it in page content is not needed
            // for these structural tests.)
            PdfDictionary xobjects = page.Resources.Elements.GetDictionary("/XObject")
                ?? AddInlineDictionary(document, page.Resources, "/XObject");
            xobjects.Elements["/Wm"] = watermark.Reference;
        }

        string path = Path.Combine(Path.GetTempPath(), "yks-wm-" + Guid.NewGuid().ToString("N") + ".pdf");
        document.Save(path);
        return path;
    }

    private static PdfDictionary NewForm(PdfDocument document, Action<PdfDictionary> configure)
    {
        var form = new PdfDictionary(document);
        document.Internals.AddObject(form);
        form.Elements.SetName("/Type", "/XObject");
        form.Elements.SetName("/Subtype", "/Form");
        form.Elements["/BBox"] = new PdfArray(document,
            new PdfItem[] { new PdfReal(0), new PdfReal(0), new PdfReal(595), new PdfReal(842) });
        configure(form);
        return form;
    }

    private static void Tag(PdfDictionary form)
    {
        PdfDocument document = form.Owner;
        var adbe = new PdfDictionary(document);
        adbe.Elements.SetName("/Private", "/Watermark");
        var pieceInfo = new PdfDictionary(document);
        pieceInfo.Elements["/ADBE_CompoundType"] = adbe;
        form.Elements["/PieceInfo"] = pieceInfo;
    }

    private static void SetContent(PdfDictionary form, string content) =>
        form.CreateStream(Encoding.ASCII.GetBytes(content));

    private static PdfDictionary AddInlineDictionary(PdfDocument document, PdfDictionary parent, string key)
    {
        var dict = new PdfDictionary(document);
        parent.Elements[key] = dict;
        return dict;
    }
}
