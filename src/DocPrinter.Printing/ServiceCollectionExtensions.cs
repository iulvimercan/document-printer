using Microsoft.Extensions.DependencyInjection;
using DocPrinter.Core.Abstractions;

namespace DocPrinter.Printing;

/// <summary>DI registration for the guided-flip duplex printer and its section detector.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ISectionDetector"/> and <see cref="IDuplexPrinter"/> (the SumatraPDF
    /// implementation), plus <see cref="PrintingOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional hook to set the printer name, flip order, or exe path.</param>
    public static IServiceCollection AddPrinting(
        this IServiceCollection services, Action<PrintingOptions>? configure = null)
    {
        services.AddOptions<PrintingOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<ISectionDetector, SectionDetector>();
        services.AddSingleton<IDuplexPrinter, SumatraPdfPrinter>();
        return services;
    }
}
