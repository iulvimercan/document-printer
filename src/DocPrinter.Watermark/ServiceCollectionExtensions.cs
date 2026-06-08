using Microsoft.Extensions.DependencyInjection;
using DocPrinter.Core.Abstractions;

namespace DocPrinter.Watermark;

/// <summary>DI registration for the custom watermark remover.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IWatermarkRemover"/> (the PDFsharp text-overlay remover).</summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddWatermark(this IServiceCollection services)
    {
        services.AddSingleton<IWatermarkRemover, PdfWatermarkRemover>();
        return services;
    }
}
