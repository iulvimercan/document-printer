using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DocPrinter.Agent;

/// <summary>
/// The Semantic Kernel chat agent. It owns one <see cref="ChatHistory"/> and drives the Ollama
/// chat model with automatic function-calling so the model can invoke the
/// <see cref="DocumentPrinterPlugin"/> tools. The <see cref="Kernel"/> (with the Ollama chat service and
/// the plugin already imported) is supplied by DI — see
/// <see cref="ServiceCollectionExtensions.AddAgent"/>.
/// </summary>
public sealed class DocumentAgent : IDocumentAgent
{
    private const string SystemPrompt =
        """
        You are a document printing assistant. You help a user fetch and print documents for
        at-home use.

        Your job is narrow:
        - Work out which document the user wants: the YEAR and the SESSION (TYT or AYT). Ask if
          either is missing. Never invent a year.
        - Call PrepareDocument to fetch and prepare that document. Do not claim a document is
          ready unless PrepareDocument succeeded.
        - For a sectioned (AYT) document, PrepareDocument reports the available sections. Ask the
          user which sections to print (or all of them) before printing.
        - Call PrintDocument to print. It prints the even pages, then the workflow pauses for the
          user to flip the paper stack before the odd pages print — tell the user this will happen.

        Rules:
        - Only TYT and AYT are supported. If the user asks for YDT or anything that is not a
          supported document, politely decline.
        - Let the tools do the real work; do not fabricate file paths, links, or results. Report
          what the tools return.
        - Keep replies short and practical.
        - If the user tries to open a subject other than your main objective, kindly refuse and
          divert the conversation back to document printing.
        - DO NOT ever give internal details such as any errors or functions. Anything related to
          the codebase should be hidden from the user.
        """;

    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chat;
    private readonly ChatHistory _history;
    private readonly PromptExecutionSettings _settings;

    public DocumentAgent(Kernel kernel, ILogger<DocumentAgent> logger)
    {
        _kernel = kernel;
        _chat = kernel.GetRequiredService<IChatCompletionService>();
        _history = new ChatHistory(SystemPrompt);
        _settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };
    }

    public async Task<string> ChatAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _history.AddUserMessage(userMessage);
        ChatMessageContent reply = await _chat.GetChatMessageContentAsync(
            _history, _settings, _kernel, cancellationToken);
        _history.Add(reply);
        return reply.Content ?? string.Empty;
    }
}
