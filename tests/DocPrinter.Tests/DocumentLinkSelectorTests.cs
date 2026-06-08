using DocPrinter.Core.Models;
using DocPrinter.Downloader;

namespace DocPrinter.Tests;

public class DocumentLinkSelectorTests
{
    // --- End-to-end over real fixtures (parser + selector) ---

    [Fact]
    public async Task Selects_2023_year_page_from_the_dropdown()
    {
        var years = await WebPageParser.ParseYearOptionsAsync(Fixtures.YearPageHtml(), Fixtures.YearPageUrl);

        var chosen = DocumentLinkSelector.SelectYear(years, 2023);

        Assert.Equal("https://www.osym.gov.tr/TR,25587/2023.html", chosen.Href);
    }

    [Fact]
    public async Task Selects_exam_group_link_on_the_year_page()
    {
        var links = await WebPageParser.ParseLinksAsync(Fixtures.YearPageHtml(), Fixtures.YearPageUrl);

        var chosen = DocumentLinkSelector.SelectDocumentGroup(links, 2025);

        Assert.Contains("TR,33280", chosen.Href);
    }

    [Theory]
    [InlineData(DocumentSession.Standard, "yks_tyt_2025_kitapcik_d250.pdf")]
    [InlineData(DocumentSession.Sectioned, "yks_ayt_2025_kitapcik_st12.pdf")]
    public async Task Selects_the_correct_session_pdf(DocumentSession session, string expectedFile)
    {
        var links = await WebPageParser.ParseLinksAsync(Fixtures.DocumentGroupPageHtml(), Fixtures.DocumentGroupPageUrl);

        var chosen = DocumentLinkSelector.SelectDocumentPdf(links, new DocumentRequest(2025, session));

        Assert.EndsWith(expectedFile, chosen.Href);
    }

    // --- Pure edge cases over hand-built candidate lists ---

    [Fact]
    public void EnsureSupported_rejects_YDT()
    {
        Assert.Throws<NotSupportedException>(() =>
            DocumentLinkSelector.EnsureSupported(new DocumentRequest(2025, DocumentSession.Specialized)));
    }

    [Fact]
    public void SelectDocumentPdf_rejects_YDT_before_matching()
    {
        var links = new List<CandidateLink>
        {
            new("YDT İngilizce", "https://dokuman.osym.gov.tr/pdfdokuman/2025/YKS/TSK/yks_ydt_ing_2025.pdf"),
        };

        Assert.Throws<NotSupportedException>(() =>
            DocumentLinkSelector.SelectDocumentPdf(links, new DocumentRequest(2025, DocumentSession.Specialized)));
    }

    [Fact]
    public void SelectDocumentPdf_throws_when_nothing_matches()
    {
        var links = new List<CandidateLink>
        {
            new("AYT", "https://dokuman.osym.gov.tr/pdfdokuman/2025/YKS/TSK/yks_ayt_2025.pdf"),
        };

        Assert.Throws<InvalidOperationException>(() =>
            DocumentLinkSelector.SelectDocumentPdf(links, new DocumentRequest(2025, DocumentSession.Standard)));
    }

    [Fact]
    public void SelectDocumentPdf_throws_on_ambiguous_match()
    {
        var links = new List<CandidateLink>
        {
            new("TYT A", "https://dokuman.osym.gov.tr/pdfdokuman/2018/YKS/TSK/yks_tyt_2018_a.pdf"),
            new("TYT B", "https://dokuman.osym.gov.tr/pdfdokuman/2018/YKS/TSK/yks_tyt_2018_b.pdf"),
        };

        Assert.Throws<InvalidOperationException>(() =>
            DocumentLinkSelector.SelectDocumentPdf(links, new DocumentRequest(2018, DocumentSession.Standard)));
    }

    [Fact]
    public void SelectDocumentPdf_uses_booklet_to_disambiguate()
    {
        var links = new List<CandidateLink>
        {
            new("TYT A", "https://dokuman.osym.gov.tr/pdfdokuman/2018/YKS/TSK/yks_tyt_2018_a.pdf"),
            new("TYT B", "https://dokuman.osym.gov.tr/pdfdokuman/2018/YKS/TSK/yks_tyt_2018_b.pdf"),
        };

        var chosen = DocumentLinkSelector.SelectDocumentPdf(links, new DocumentRequest(2018, DocumentSession.Standard, Booklet: "b"));

        Assert.EndsWith("yks_tyt_2018_b.pdf", chosen.Href);
    }
}
