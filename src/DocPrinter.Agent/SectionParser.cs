using System.Text;
using DocPrinter.Core.Models;

namespace DocPrinter.Agent;

/// <summary>
/// Turns a free-text AYT section request ("matematik ve fen", "edebiyat", "all") into a typed
/// <see cref="PrintSelection"/>. Matching is Turkish-fold-insensitive so "MATEMATİK" and
/// "matematik" both resolve. Empty input or an "all"/"hepsi"-style word selects every section.
/// </summary>
public static class SectionParser
{
    private static readonly string[] AllWords = ["all", "hepsi", "tumu", "tum", "all sections"];

    /// <summary>
    /// Parses <paramref name="value"/> into a <see cref="PrintSelection"/>. Null/empty or an
    /// "all"-style word means every section.
    /// </summary>
    /// <exception cref="ArgumentException">No section could be recognized in a non-empty value.</exception>
    public static PrintSelection Parse(string? value)
    {
        string folded = Fold(value);
        if (folded.Length == 0 || Array.IndexOf(AllWords, folded) >= 0)
        {
            return PrintSelection.All;
        }

        var sections = new HashSet<DocumentSection>();

        if (folded.Contains("matematik") || HasWord(folded, "mat"))
        {
            sections.Add(DocumentSection.Mathematics);
        }

        if (folded.Contains("fen"))
        {
            sections.Add(DocumentSection.Science);
        }

        if (folded.Contains("sosyal") && folded.Contains('2'))
        {
            sections.Add(DocumentSection.SocialSciences2);
        }

        if (folded.Contains("edebiyat")
            || folded.Contains("turk dili")
            || (folded.Contains("sosyal") && folded.Contains('1')))
        {
            sections.Add(DocumentSection.LiteratureAndSocialSciences1);
        }

        if (sections.Count == 0)
        {
            throw new ArgumentException(
                $"Could not recognize any AYT section in '{value}'. Valid sections are: " +
                "Türk Dili ve Edebiyat / Sosyal Bilimler-1, Sosyal Bilimler-2, Matematik, Fen.",
                nameof(value));
        }

        return new PrintSelection(sections);
    }

    /// <summary>
    /// Normalizes to lowercase ASCII, folding Turkish diacritics (ı/İ→i, ş→s, ç→c, ö→o, ü→u, ğ→g)
    /// and collapsing whitespace, so keyword matching is accent- and case-insensitive.
    /// </summary>
    private static string Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        bool lastWasSpace = false;
        foreach (char raw in value.Trim())
        {
            char c = char.ToLowerInvariant(raw) switch
            {
                'ı' or 'İ' or 'i' => 'i',
                'ş' => 's',
                'ç' => 'c',
                'ö' => 'o',
                'ü' => 'u',
                'ğ' => 'g',
                var other => other,
            };

            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                lastWasSpace = true;
                continue;
            }

            lastWasSpace = false;
            builder.Append(c);
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>True when <paramref name="token"/> appears in <paramref name="text"/> as a whole word.</summary>
    private static bool HasWord(string text, string token)
    {
        int index = text.IndexOf(token, StringComparison.Ordinal);
        while (index >= 0)
        {
            bool leftOk = index == 0 || text[index - 1] == ' ';
            int end = index + token.Length;
            bool rightOk = end == text.Length || text[end] == ' ';
            if (leftOk && rightOk)
            {
                return true;
            }

            index = text.IndexOf(token, index + 1, StringComparison.Ordinal);
        }

        return false;
    }
}
