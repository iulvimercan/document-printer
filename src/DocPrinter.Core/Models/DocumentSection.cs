namespace DocPrinter.Core.Models;

/// <summary>
/// The four AYT section tests, in booklet order. Each is independently selectable for printing.
/// TYT has no equivalent — all of its sections are always printed.
/// </summary>
public enum DocumentSection
{
    /// <summary>"TÜRK DİLİ ve EDEBİYATI-SOSYAL BİLİMLER-1 TESTİ".</summary>
    LiteratureAndSocialSciences1,

    /// <summary>"SOSYAL BİLİMLER-2 TESTİ".</summary>
    SocialSciences2,

    /// <summary>"MATEMATİK TESTİ".</summary>
    Mathematics,

    /// <summary>"FEN BİLİMLERİ TESTİ".</summary>
    Science,
}
