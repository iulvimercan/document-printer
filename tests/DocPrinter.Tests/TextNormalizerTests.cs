using DocPrinter.Printing;

namespace DocPrinter.Tests;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("FEN BİLİMLERİ TESTİ", "FENBILIMLERITESTI")]
    [InlineData("Matematik Testi", "MATEMATIKTESTI")]
    [InlineData("SOSYAL BİLİMLER-2 TESTİ", "SOSYALBILIMLER2TESTI")]
    [InlineData("Türk Dili ve Edebiyatı", "TURKDILIVEEDEBIYATI")]
    [InlineData("Coğrafya, Şekil, Çözüm, Öğe, Ünite", "COGRAFYASEKILCOZUMOGEUNITE")]
    public void Fold_strips_diacritics_spacing_and_punctuation(string input, string expected) =>
        Assert.Equal(expected, TextNormalizer.Fold(input));

    [Fact]
    public void Fold_folds_dotted_and_dotless_i_to_the_same_letter() =>
        Assert.Equal(TextNormalizer.Fold("İ"), TextNormalizer.Fold("ı"));
}
