namespace DocPrinter.Agent;

/// <summary>
/// The conversational entry point to the document printer. Maintains a single chat session and lets the
/// underlying LLM call the <see cref="DocumentPrinterPlugin"/> tools to download, de-watermark, and
/// print a document from plain-language requests like "print 2025 TYT".
/// </summary>
public interface IDocumentAgent
{
    /// <summary>
    /// Sends <paramref name="userMessage"/> to the agent and returns its reply. Tool calls (the
    /// download/print pipeline) are invoked automatically as part of producing the reply.
    /// </summary>
    Task<string> ChatAsync(string userMessage, CancellationToken cancellationToken = default);
}
