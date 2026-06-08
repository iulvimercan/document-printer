using DocPrinter.Agent;
using DocPrinter.Core.Models;

namespace DocPrinter.Tests;

public class SectionParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("all")]
    [InlineData("hepsi")]
    [InlineData("tümü")]
    public void Empty_or_all_selects_every_section(string? value)
    {
        PrintSelection selection = SectionParser.Parse(value);

        Assert.Null(selection.IncludedSections); // null == include everything
        Assert.True(selection.Includes(DocumentSection.Mathematics));
        Assert.True(selection.Includes(DocumentSection.Science));
    }

    [Theory]
    [InlineData("matematik", DocumentSection.Mathematics)]
    [InlineData("MATEMATİK", DocumentSection.Mathematics)]
    [InlineData("mat", DocumentSection.Mathematics)]
    [InlineData("fen", DocumentSection.Science)]
    [InlineData("Fen Bilimleri", DocumentSection.Science)]
    [InlineData("edebiyat", DocumentSection.LiteratureAndSocialSciences1)]
    [InlineData("Türk Dili", DocumentSection.LiteratureAndSocialSciences1)]
    [InlineData("sosyal 1", DocumentSection.LiteratureAndSocialSciences1)]
    [InlineData("sosyal 2", DocumentSection.SocialSciences2)]
    [InlineData("Sosyal Bilimler-2", DocumentSection.SocialSciences2)]
    public void Parses_single_sections_accent_insensitively(string value, DocumentSection expected)
    {
        PrintSelection selection = SectionParser.Parse(value);

        Assert.NotNull(selection.IncludedSections);
        Assert.Single(selection.IncludedSections!);
        Assert.True(selection.Includes(expected));
    }

    [Fact]
    public void Parses_multiple_comma_separated_sections()
    {
        PrintSelection selection = SectionParser.Parse("matematik, fen");

        Assert.NotNull(selection.IncludedSections);
        Assert.Equal(2, selection.IncludedSections!.Count);
        Assert.True(selection.Includes(DocumentSection.Mathematics));
        Assert.True(selection.Includes(DocumentSection.Science));
        Assert.False(selection.Includes(DocumentSection.SocialSciences2));
    }

    [Fact]
    public void Distinguishes_social_one_from_social_two()
    {
        PrintSelection selection = SectionParser.Parse("sosyal 1 ve sosyal 2");

        Assert.True(selection.Includes(DocumentSection.LiteratureAndSocialSciences1));
        Assert.True(selection.Includes(DocumentSection.SocialSciences2));
    }

    [Fact]
    public void Throws_when_nothing_recognized()
    {
        Assert.Throws<ArgumentException>(() => SectionParser.Parse("biyoloji kimya"));
    }
}
