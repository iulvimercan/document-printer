using DocPrinter.Core.Models;
using DocPrinter.Printing;

namespace DocPrinter.Tests;

public class DuplexPlanTests
{
    private static DocumentLayout Standard(int pages) =>
        new(pages, pages, pages - 1, Array.Empty<DocumentSectionRange>());

    private static DocumentLayout Sectioned() => new(12, 12, 11, new[]
    {
        new DocumentSectionRange(DocumentSection.LiteratureAndSocialSciences1, 3, 5),
        new DocumentSectionRange(DocumentSection.SocialSciences2, 6, 7),
        new DocumentSectionRange(DocumentSection.Mathematics, 8, 9),
        new DocumentSectionRange(DocumentSection.Science, 10, 10),
    });

    [Fact]
    public void Whole_exam_drops_instructions_page_and_keeps_the_answer_key()
    {
        DuplexPlan plan = DuplexPlan.Build(Standard(43), PrintSelection.All, reverseSecondPass: false);

        Assert.DoesNotContain(42, plan.Pages);     // instructions page dropped
        Assert.Contains(43, plan.Pages);           // answer key kept
        Assert.Equal(42, plan.Pages.Count);
    }

    [Fact]
    public void Final_page_count_is_always_even()
    {
        // Base set is odd here (1,2,4 after dropping the instructions page 3), so the instructions
        // page is re-added to reach an even total.
        DuplexPlan plan = DuplexPlan.Build(Standard(4), PrintSelection.All, reverseSecondPass: false);

        Assert.Equal(new[] { 1, 2, 3, 4 }, plan.Pages);
        Assert.Equal(0, plan.Pages.Count % 2);
    }

    [Fact]
    public void Excluding_a_section_removes_its_pages_but_keeps_answer_key_and_stays_even()
    {
        var selection = new PrintSelection(new HashSet<DocumentSection>
        {
            DocumentSection.LiteratureAndSocialSciences1,
            DocumentSection.SocialSciences2,
            DocumentSection.Science,
        }); // Mathematics excluded

        DuplexPlan plan = DuplexPlan.Build(Sectioned(), selection, reverseSecondPass: false);

        Assert.DoesNotContain(8, plan.Pages);
        Assert.DoesNotContain(9, plan.Pages);
        Assert.Contains(12, plan.Pages);
        Assert.Equal(0, plan.Pages.Count % 2);
    }

    [Fact]
    public void Passes_re_index_within_the_selection_and_have_equal_size()
    {
        DuplexPlan plan = DuplexPlan.Build(Standard(6), PrintSelection.All, reverseSecondPass: false);

        // Pages = 1..6. Even pass = even positions, odd pass = odd positions.
        Assert.Equal(new[] { 2, 4, 6 }, plan.EvenPass);
        Assert.Equal(new[] { 1, 3, 5 }, plan.OddPass);
        Assert.Equal(plan.EvenPass.Count, plan.OddPass.Count);
    }

    [Fact]
    public void Reverse_second_pass_reverses_only_the_odd_pass()
    {
        DuplexPlan plan = DuplexPlan.Build(Standard(6), PrintSelection.All, reverseSecondPass: true);

        Assert.Equal(new[] { 2, 4, 6 }, plan.EvenPass);   // unchanged
        Assert.Equal(new[] { 5, 3, 1 }, plan.OddPass);    // reversed
    }
}
