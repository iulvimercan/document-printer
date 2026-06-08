using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Pdf;

namespace DocPrinter.Watermark;

/// <summary>
/// Locates the watermark Form XObjects inside a document PDF using two complementary,
/// structure-driven strategies. Nothing here is tied to a particular year, page count, or
/// the exact constants of any one sample — detection keys off PDF structure so it survives
/// the content/length differences between documents (TYT vs AYT, older years).
/// <list type="bullet">
/// <item>The Adobe <c>/PieceInfo → /ADBE_CompoundType → /Private /Watermark</c> tag that
/// Acrobat stamps on generated watermark artwork.</item>
/// <item>The diagonal "rotated transform then <c>Do</c>" shape of the per-page overlay forms,
/// matched at any angle.</item>
/// </list>
/// </summary>
public static partial class WatermarkDetector
{
    // The shear terms (b, c) of a cm matrix are ~0 for an axis-aligned scale/translate and
    // clearly non-zero for a rotated/skewed transform such as the diagonal watermark stamp.
    private const double RotationEpsilon = 0.01;

    /// <summary>Returns every watermark Form XObject whose content should be emptied.</summary>
    public static IReadOnlyList<PdfDictionary> Find(PdfDocument document)
    {
        var targets = new List<PdfDictionary>();
        var seen = new HashSet<PdfDictionary>();

        foreach (PdfObject obj in document.Internals.GetAllObjects())
        {
            if (obj is not PdfDictionary dict || !IsFormXObject(dict))
            {
                continue;
            }

            if ((IsTaggedWatermark(dict) || IsDiagonalOverlay(dict)) && seen.Add(dict))
            {
                targets.Add(dict);
            }
        }

        return targets;
    }

    private static bool IsFormXObject(PdfDictionary dict) =>
        dict.Stream is not null && dict.Elements.GetName("/Subtype") == "/Form";

    /// <summary>Adobe marks generated watermarks with /PieceInfo → /ADBE_CompoundType → /Private /Watermark.</summary>
    private static bool IsTaggedWatermark(PdfDictionary dict)
    {
        PdfDictionary? pieceInfo = dict.Elements.GetDictionary("/PieceInfo");
        PdfDictionary? adbe = pieceInfo?.Elements.GetDictionary("/ADBE_CompoundType");
        return adbe?.Elements.GetName("/Private") == "/Watermark";
    }

    /// <summary>
    /// True when the form paints something under a rotated/skewed transform — the signature of
    /// the diagonal overlay. Angle-agnostic: any non-axis-aligned <c>cm</c> plus a <c>Do</c>.
    /// </summary>
    private static bool IsDiagonalOverlay(PdfDictionary dict)
    {
        string content = Encoding.Latin1.GetString(dict.Stream!.UnfilteredValue);
        if (!content.Contains("Do"))
        {
            return false;
        }

        foreach (Match match in CmMatrix().Matches(content))
        {
            double b = Parse(match.Groups[2].Value);
            double c = Parse(match.Groups[3].Value);
            if (Math.Abs(b) > RotationEpsilon || Math.Abs(c) > RotationEpsilon)
            {
                return true;
            }
        }

        return false;
    }

    private static double Parse(string value) =>
        double.Parse(value, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"(-?\d*\.?\d+)\s+(-?\d*\.?\d+)\s+(-?\d*\.?\d+)\s+(-?\d*\.?\d+)\s+(-?\d*\.?\d+)\s+(-?\d*\.?\d+)\s+cm")]
    private static partial Regex CmMatrix();
}
