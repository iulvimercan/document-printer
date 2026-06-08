namespace DocPrinter.Printing;

/// <summary>
/// Configuration for the guided-flip duplex printer.
/// </summary>
public sealed class PrintingOptions
{
    /// <summary>
    /// The target printer name (e.g. <c>"Microsoft Print to PDF"</c>). When null/empty the
    /// Windows default printer is used.
    /// </summary>
    public string? PrinterName { get; set; }

    /// <summary>
    /// When true, the second (odd) pass is sent in reverse page order. Needed for printers whose
    /// flipped-stack orientation reverses the sheet order; the default suits same-order stackers.
    /// </summary>
    public bool ReverseSecondPass { get; set; }

    /// <summary>
    /// Path to <c>SumatraPDF.exe</c>. When null/empty the bundled copy under
    /// <c>tools/SumatraPDF/</c> beside the application is used.
    /// </summary>
    public string? SumatraPath { get; set; }
}
