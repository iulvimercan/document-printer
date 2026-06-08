using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using DocPrinter.Core.Models;
using DocPrinter.Watermark;

namespace DocPrinter.Tests;

/// <summary>
/// Offline tests over synthetic PDFs (<see cref="SyntheticPdf"/>). Detection and removal are
/// asserted structurally — nothing depends on a fixed page count or angle, matching the
/// remover's design.
/// </summary>
public class WatermarkRemoverTests
{
    private static readonly DocumentRequest Request = new(2025, DocumentSession.Standard);

    private static PdfWatermarkRemover Remover => new(NullLogger<PdfWatermarkRemover>.Instance);

    [Fact]
    public void Detects_diagonal_overlay_form()
    {
        string source = SyntheticPdf.DiagonalOverlay(pages: 1, SyntheticPdf.Rotation45);
        try
        {
            using PdfDocument document = PdfReader.Open(source, PdfDocumentOpenMode.Modify);
            Assert.NotEmpty(WatermarkDetector.Find(document));
        }
        finally { Delete(source); }
    }

    [Fact]
    public void Detects_overlay_at_a_non_45_degree_angle()
    {
        string source = SyntheticPdf.DiagonalOverlay(pages: 1, SyntheticPdf.Rotation30);
        try
        {
            using PdfDocument document = PdfReader.Open(source, PdfDocumentOpenMode.Modify);
            Assert.NotEmpty(WatermarkDetector.Find(document));
        }
        finally { Delete(source); }
    }

    [Fact]
    public void Detects_tagged_watermark_form()
    {
        string source = SyntheticPdf.TaggedWatermark(pages: 1);
        try
        {
            using PdfDocument document = PdfReader.Open(source, PdfDocumentOpenMode.Modify);
            Assert.NotEmpty(WatermarkDetector.Find(document));
        }
        finally { Delete(source); }
    }

    [Fact]
    public async Task Removes_the_diagonal_overlay()
    {
        string source = SyntheticPdf.DiagonalOverlay(pages: 3, SyntheticPdf.Rotation45);
        string destination = TempPdf();
        try
        {
            DocumentFile result = await Remover.RemoveAsync(new DocumentFile(source, Request), destination);

            Assert.True(result.IsWatermarkRemoved);
            Assert.Equal(destination, result.Path);

            using PdfDocument output = PdfReader.Open(destination, PdfDocumentOpenMode.Modify);
            Assert.Empty(WatermarkDetector.Find(output)); // rotation gone, nothing left to match
        }
        finally { Delete(source); Delete(destination); }
    }

    [Fact]
    public async Task Empties_the_tagged_watermark_content()
    {
        string source = SyntheticPdf.TaggedWatermark(pages: 2);
        string destination = TempPdf();
        try
        {
            await Remover.RemoveAsync(new DocumentFile(source, Request), destination);

            using PdfDocument output = PdfReader.Open(destination, PdfDocumentOpenMode.Modify);
            PdfDictionary? tagged = FindTaggedForm(output);
            Assert.NotNull(tagged);
            Assert.True(tagged!.Stream is null || tagged.Stream.UnfilteredValue.Length == 0);
        }
        finally { Delete(source); Delete(destination); }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(12)]
    public async Task Preserves_the_page_count(int pages)
    {
        string source = SyntheticPdf.DiagonalOverlay(pages, SyntheticPdf.Rotation45);
        string destination = TempPdf();
        try
        {
            await Remover.RemoveAsync(new DocumentFile(source, Request), destination);

            using PdfDocument output = PdfReader.Open(destination, PdfDocumentOpenMode.Modify);
            Assert.Equal(pages, output.PageCount);
        }
        finally { Delete(source); Delete(destination); }
    }

    [Fact]
    public async Task Throws_when_no_watermark_is_present()
    {
        string source = SyntheticPdf.WithoutWatermark(pages: 2);
        string destination = TempPdf();
        try
        {
            await Assert.ThrowsAsync<WatermarkNotFoundException>(
                () => Remover.RemoveAsync(new DocumentFile(source, Request), destination));
        }
        finally { Delete(source); Delete(destination); }
    }

    // Locates the watermark form by its Adobe tag without requiring a stream (the stream may be
    // emptied or dropped after removal).
    private static PdfDictionary? FindTaggedForm(PdfDocument document) =>
        document.Internals.GetAllObjects()
            .OfType<PdfDictionary>()
            .FirstOrDefault(d => d.Elements.GetDictionary("/PieceInfo")?
                .Elements.GetDictionary("/ADBE_CompoundType")?
                .Elements.GetName("/Private") == "/Watermark");

    private static string TempPdf() =>
        Path.Combine(Path.GetTempPath(), "yks-clean-" + Guid.NewGuid().ToString("N") + ".pdf");

    private static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
