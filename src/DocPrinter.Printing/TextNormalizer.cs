using System.Text;

namespace DocPrinter.Printing;

/// <summary>
/// Folds Turkish document header text into a comparison-friendly form so header matching survives
/// case, diacritics, and the PDF-extraction quirks of dotted/dotless I. Produces an uppercase,
/// ASCII-only string with all spacing and punctuation stripped (e.g. "Fen Bilimleri Testi" and
/// "FEN BİLİMLERİ TESTİ" both fold to "FENBILIMLERITESTI").
/// </summary>
internal static class TextNormalizer
{
    public static string Fold(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char raw in text)
        {
            char c = char.ToUpperInvariant(raw);
            char mapped = c switch
            {
                // 'ı' is the dotless ı, whose invariant upper-case is itself, not 'I'.
                'İ' or 'I' or 'ı' or 'Î' => 'I',
                'Ş' => 'S',
                'Ç' => 'C',
                'Ö' => 'O',
                'Ü' => 'U',
                'Ğ' => 'G',
                'Â' => 'A',
                'Û' => 'U',
                _ => c,
            };

            if (mapped is (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
            {
                builder.Append(mapped);
            }
        }

        return builder.ToString();
    }
}
