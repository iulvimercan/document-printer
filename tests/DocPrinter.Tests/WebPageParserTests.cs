using DocPrinter.Downloader;

namespace DocPrinter.Tests;

public class WebPageParserTests
{
    [Fact]
    public async Task ParseYearOptions_extracts_all_years_with_absolute_urls()
    {
        var years = await WebPageParser.ParseYearOptionsAsync(Fixtures.YearPageHtml(), Fixtures.YearPageUrl);

        Assert.Equal(9, years.Count);
        Assert.Equal(
            ["2025", "2024", "2023", "2022", "2021", "2020", "2019", "2018", "2017"],
            years.Select(y => y.Text));

        var year2024 = years.Single(y => y.Text == "2024");
        Assert.Equal("https://www.osym.gov.tr/TR,29435/2024.html", year2024.Href);
    }

    [Fact]
    public async Task ParseLinks_finds_the_exam_group_link_on_the_year_page()
    {
        var links = await WebPageParser.ParseLinksAsync(Fixtures.YearPageHtml(), Fixtures.YearPageUrl);

        Assert.Contains(links, l =>
            l.Href == "https://www.osym.gov.tr/TR,33280/2025-yks-tyt-ayt-ve-ydt-temel-soru-kitapciklari-ve-cevap-anahtarlari.html");
    }

    [Fact]
    public async Task ParseLinks_finds_the_session_pdf_links_on_the_exam_group_page()
    {
        var links = await WebPageParser.ParseLinksAsync(Fixtures.DocumentGroupPageHtml(), Fixtures.DocumentGroupPageUrl);

        Assert.Contains(links, l => l.Href == "https://dokuman.osym.gov.tr/pdfdokuman/2025/YKS/TSK/yks_tyt_2025_kitapcik_d250.pdf");
        Assert.Contains(links, l => l.Href == "https://dokuman.osym.gov.tr/pdfdokuman/2025/YKS/TSK/yks_ayt_2025_kitapcik_st12.pdf");
    }
}
