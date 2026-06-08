using Microsoft.Extensions.Logging.Abstractions;
using DocPrinter.Core.Models;
using DocPrinter.Printing;

namespace DocPrinter.Tests;

public class SectionDetectorTests
{
    private static readonly SectionDetector Detector = new(NullLogger<SectionDetector>.Instance);

    private static DocumentFile File(string path, DocumentSession session) =>
        new(path, new DocumentRequest(2024, session));

    [Fact]
    public async Task Sectioned_finds_four_sections_with_contiguous_ranges()
    {
        string path = SyntheticDocumentPdf.Sectioned();
        try
        {
            DocumentLayout layout = await Detector.DetectAsync(File(path, DocumentSession.Sectioned));

            Assert.Equal(12, layout.PageCount);
            Assert.Equal(12, layout.AnswerKeyPage);
            Assert.Equal(11, layout.InstructionPage);

            Assert.Collection(layout.Sections,
                s => Assert.Equal((DocumentSection.LiteratureAndSocialSciences1, 3, 5), (s.Section, s.StartPage, s.EndPage)),
                s => Assert.Equal((DocumentSection.SocialSciences2, 6, 7), (s.Section, s.StartPage, s.EndPage)),
                s => Assert.Equal((DocumentSection.Mathematics, 8, 9), (s.Section, s.StartPage, s.EndPage)),
                s => Assert.Equal((DocumentSection.Science, 10, 10), (s.Section, s.StartPage, s.EndPage)));
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Fact]
    public async Task Standard_is_not_split_into_sections()
    {
        string path = SyntheticDocumentPdf.Standard();
        try
        {
            DocumentLayout layout = await Detector.DetectAsync(File(path, DocumentSession.Standard));

            Assert.Equal(6, layout.PageCount);
            Assert.Equal(6, layout.AnswerKeyPage);
            Assert.Equal(5, layout.InstructionPage);
            Assert.Empty(layout.Sections);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Fact]
    public async Task Header_match_is_case_insensitive()
    {
        string path = SyntheticDocumentPdf.Build(4, new Dictionary<int, string[]>
        {
            [2] = new[] { "matematik testi" },
        });
        try
        {
            DocumentLayout layout = await Detector.DetectAsync(File(path, DocumentSession.Sectioned));

            DocumentSectionRange section = Assert.Single(layout.Sections);
            Assert.Equal(DocumentSection.Mathematics, section.Section);
            Assert.Equal(2, section.StartPage);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }
}
