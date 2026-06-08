namespace DocPrinter.Core.Models;

/// <summary>
/// The supported document session variants: <see cref="Standard"/> (printed whole),
/// <see cref="Sectioned"/> (has selectable sections), and <see cref="Specialized"/> (a separate
/// variant that is not currently supported).
/// </summary>
public enum DocumentSession
{
    Standard,
    Sectioned,
    Specialized,
}
