namespace DocPrinter.Printing;

/// <summary>
/// Thrown when the SumatraPDF executable that drives printing cannot be located.
/// Printing fails loudly rather than silently doing nothing.
/// </summary>
public sealed class SumatraPdfNotFoundException(string searchedPath)
    : Exception(
        $"SumatraPDF.exe was not found at '{searchedPath}'. Bundle the portable executable under " +
        "tools/SumatraPDF/ or set PrintingOptions.SumatraPath to its location.")
{
    /// <summary>The path that was probed.</summary>
    public string SearchedPath { get; } = searchedPath;
}
