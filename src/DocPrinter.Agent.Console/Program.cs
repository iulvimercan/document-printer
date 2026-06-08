using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DocPrinter.Agent;
using DocPrinter.Agent.Abstractions;
using DocPrinter.Agent.Console;
using DocPrinter.Downloader;

// A tiny REPL for driving the agent end-to-end before the Blazor UI (Step 6). Model and endpoint
// can be overridden with the DOC_MODEL / DOC_OLLAMA_ENDPOINT environment variables.
using IHost host = Host.CreateDefaultBuilder(args)
    // Source URLs live in User Secrets, not in the repo. Load them explicitly so they are picked up
    // regardless of the hosting environment.
    .ConfigureAppConfiguration((_, config) => config.AddUserSecrets(typeof(Program).Assembly, optional: true))
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(o => o.SingleLine = true);
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((context, services) =>
    {
        // Bind the downloader's source URLs from the "Downloader" section (User Secrets).
        services.Configure<DownloaderOptions>(o => context.Configuration.GetSection("Downloader").Bind(o));

        services.AddAgent(options =>
        {
            string? model = Environment.GetEnvironmentVariable("DOC_MODEL");
            if (!string.IsNullOrWhiteSpace(model))
            {
                options.ModelId = model;
            }

            string? endpoint = Environment.GetEnvironmentVariable("DOC_OLLAMA_ENDPOINT");
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                options.Endpoint = endpoint;
            }

            string? key = Environment.GetEnvironmentVariable("DOC_KEY");
            if (!string.IsNullOrWhiteSpace(key))
            {
                options.Key = key;
            }
        });

        services.AddSingleton<IFlipPrompt, ConsoleFlipPrompt>();
    })
    .Build();

IDocumentAgent agent = host.Services.GetRequiredService<IDocumentAgent>();

Console.WriteLine("Document Printer — chat with the agent. Try: \"print 2025 TYT\".");
Console.WriteLine("Type 'exit' or 'quit' to leave.");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

while (!cts.IsCancellationRequested)
{
    Console.Write("you> ");
    string? line = Console.ReadLine();
    if (line is null)
    {
        break;
    }

    string message = line.Trim();
    if (message.Length == 0)
    {
        continue;
    }

    if (message is "exit" or "quit")
    {
        break;
    }

    try
    {
        string reply = await agent.ChatAsync(message, cts.Token);
        Console.WriteLine($"bot> {reply}");
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"bot> Something went wrong: {ex.Message}");
    }

    Console.WriteLine();
}

Console.WriteLine("Bye.");