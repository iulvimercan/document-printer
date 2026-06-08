using Microsoft.Extensions.DependencyInjection;
using DocPrinter.Core.Abstractions;

namespace DocPrinter.Downloader;

/// <summary>DI registration for the source downloader.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IDocumentSource"/> (the source Playwright downloader) and its options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional hook to override <see cref="DownloaderOptions"/>.</param>
    public static IServiceCollection AddDownloader(
        this IServiceCollection services,
        Action<DownloaderOptions>? configure = null)
    {
        services.AddOptions<DownloaderOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<IDocumentSource, WebDocumentSource>();
        return services;
    }
}
