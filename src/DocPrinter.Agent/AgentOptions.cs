namespace DocPrinter.Agent;

/// <summary>
/// Configuration for the document agent: which local Ollama model to drive, where Ollama listens,
/// and where the pipeline writes its working files (downloaded + cleaned PDFs).
/// </summary>
public sealed class AgentOptions
{
    /// <summary>
    /// The Ollama model the agent uses for orchestration and tool-calling. Must be a model pulled
    /// locally that supports function calling. Override per host if the default proves too small to
    /// call tools reliably (e.g. <c>"llama3.1:8b"</c>).
    /// </summary>
    public string ModelId { get; set; } = "llama3.1:8b-instruct-q4_0";

    /// <summary>The Ollama HTTP endpoint. Defaults to the local Ollama daemon.</summary>
    public string Endpoint { get; set; } = "http://localhost:11434";
    
    public string Key { get; set; }

    /// <summary>
    /// Folder the pipeline downloads and cleans documents into. Defaults to a <c>DocPrinter</c> folder
    /// under the user's local application data.
    /// </summary>
    public string WorkingDirectory { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DocPrinter");
}
