using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DocPrinter.Agent;
using DocPrinter.Agent.Abstractions;
using DocPrinter.Core.Abstractions;
using DocPrinter.Core.Models;

namespace DocPrinter.Tests;

/// <summary>
/// Exercises the real Ollama-backed agent, but with the download/watermark/print pipeline replaced
/// by side-effect-free fakes — so it verifies the one thing that needs a live model: that the model
/// actually calls the tools for a request like "print 2025 TYT". No network, no printer.
/// A no-op unless <c>DOC_LIVE_TESTS=1</c> and a reachable Ollama with the model is available.
/// ⚠️ A very small model may not call tools reliably; override the model with <c>DOC_MODEL</c>
/// (e.g. <c>llama3.1:8b</c>) if this fails. Run with: <c>dotnet test --filter Category=Live</c>.
/// </summary>
[Trait("Category", "Live")]
public class DocumentAgentLiveTests
{
    [Fact]
    public async Task Agent_calls_the_pipeline_tools_for_a_print_request()
    {
        if (Environment.GetEnvironmentVariable("DOC_LIVE_TESTS") != "1")
        {
            return; // disabled by default
        }

        var printer = new FakeDuplexPrinter();
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddAgent(options =>
        {
            string? model = Environment.GetEnvironmentVariable("DOC_MODEL");
            if (!string.IsNullOrWhiteSpace(model))
            {
                options.ModelId = model;
            }
        });

        // Override the real pipeline services with fakes (last registration wins).
        services.AddSingleton<IDocumentSource, FakeDocumentSource>();
        services.AddSingleton<IWatermarkRemover, FakeWatermarkRemover>();
        services.AddSingleton<ISectionDetector>(
            new FakeSectionDetector(new DocumentLayout(43, 43, 42, Array.Empty<DocumentSectionRange>())));
        services.AddSingleton<IDuplexPrinter>(printer);
        services.AddSingleton<IFlipPrompt>(new FakeFlipPrompt());

        using ServiceProvider provider = services.BuildServiceProvider();
        IDocumentAgent agent = provider.GetRequiredService<IDocumentAgent>();

        await agent.ChatAsync("Please print the 2025 TYT exam, and go ahead and start printing.");

        Assert.NotNull(printer.LastFile); // the model drove the pipeline through to printing
    }
}
