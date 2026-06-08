using DocPrinter.Core.Models;

namespace DocPrinter.Agent;

/// <summary>
/// Maps a free-text session name ("tyt", "AYT", …) to an <see cref="DocumentSession"/>. Keeps the
/// LLM's job narrow: the model only needs to surface the word the user said; this turns it into a
/// typed value and rejects the unsupported YDT session the same way the downloader does.
/// </summary>
public static class SessionParser
{
    /// <summary>
    /// Parses <paramref name="value"/> into an <see cref="DocumentSession"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The value names no recognized session.</exception>
    /// <exception cref="NotSupportedException">The value names YDT, which is not supported.</exception>
    public static DocumentSession Parse(string? value)
    {
        string token = (value ?? string.Empty).Trim();
        return token.ToUpperInvariant() switch
        {
            "TYT" => DocumentSession.Standard,
            "AYT" => DocumentSession.Sectioned,
            "YDT" => throw new NotSupportedException(
                "The 'YDT' session is not supported yet. Please request a TYT or AYT document."),
            _ => throw new ArgumentException(
                $"'{value}' is not a recognized session. Use TYT or AYT.", nameof(value)),
        };
    }
}
