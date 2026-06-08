namespace DocPrinter.Tests;

/// <summary>Loads the saved source HTML fixtures copied next to the test assembly.</summary>
internal static class Fixtures
{
    public const string YearPageUrl = "https://www.osym.gov.tr/TR,33279/2025.html";

    public const string DocumentGroupPageUrl =
        "https://www.osym.gov.tr/TR,33280/2025-yks-tyt-ayt-ve-ydt-temel-soru-kitapciklari-ve-cevap-anahtarlari.html";

    public static string YearPageHtml() => Read("source-year-2025.html");

    public static string DocumentGroupPageHtml() => Read("source-examgroup-2025.html");

    private static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
