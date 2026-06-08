using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using DocPrinter.Agent.Abstractions;
using DocPrinter.Downloader;
using DocPrinter.Printing;
using DocPrinter.Watermark;

namespace DocPrinter.Agent;

/// <summary>DI registration for the document agent and the full pipeline it drives.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the agent (<see cref="IDocumentAgent"/>), its Semantic Kernel + Ollama chat service,
    /// the <see cref="DocumentPipeline"/>, and the underlying downloader, watermark remover, and printer.
    /// The host must additionally register an <see cref="IFlipPrompt"/> implementation for the
    /// guided-flip prompt between print passes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional hook to set the model id, endpoint, or working directory.</param>
    /// <param name="conversationLifetime">
    /// Lifetime for the per-conversation state holders (<see cref="DocumentPrinterPlugin"/>, the
    /// <see cref="KernelPlugin"/> wrapper, and <see cref="IDocumentAgent"/>). The single-conversation
    /// console uses the default <see cref="ServiceLifetime.Singleton"/>; a multi-circuit host (Blazor)
    /// passes <see cref="ServiceLifetime.Scoped"/> so concurrent conversations don't share state.
    /// </param>
    public static IServiceCollection AddAgent(
        this IServiceCollection services,
        Action<AgentOptions>? configure = null,
        ServiceLifetime conversationLifetime = ServiceLifetime.Singleton)
    {
        var options = new AgentOptions();
        configure?.Invoke(options);
        services.AddSingleton(Options.Create(options));

        services.AddDownloader();
        services.AddWatermark();
        services.AddPrinting();

        // A no-op status sink so hosts that don't surface progress (the console) still resolve the
        // optional dependency. A UI host registers its own scoped IPipelineStatus, which wins.
        services.TryAddSingleton<IProgress<PipelineActivity>>(_ => new PipelineStatus());

        services.AddSingleton<DocumentPipeline>();

        // Per-conversation state: lifetime chosen by the host.
        services.Add(new ServiceDescriptor(
            typeof(DocumentPrinterPlugin), typeof(DocumentPrinterPlugin), conversationLifetime));
        services.Add(new ServiceDescriptor(
            typeof(KernelPlugin),
            sp => KernelPluginFactory.CreateFromObject(
                sp.GetRequiredService<DocumentPrinterPlugin>(), "DocumentPrinter"),
            conversationLifetime));

        // services.AddOllamaChatCompletion(options.ModelId, new Uri(options.Endpoint));
        services.AddAzureOpenAIChatCompletion(options.ModelId, options.Endpoint, options.Key);
        services.AddKernel();

        services.Add(new ServiceDescriptor(
            typeof(IDocumentAgent), typeof(DocumentAgent), conversationLifetime));
        return services;
    }
}
