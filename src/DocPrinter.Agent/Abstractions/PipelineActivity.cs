namespace DocPrinter.Agent.Abstractions;

/// <summary>
/// The user-facing stages of a document run, surfaced live to a UI while the agent works. This is a
/// presentation concern (it spans both pipeline phases and the manual-flip pause), so it lives in
/// the Agent layer rather than in the <c>Core</c> pipeline contracts.
/// </summary>
public enum PipelineActivity
{
    /// <summary>Nothing in progress.</summary>
    Idle,

    /// <summary>Fetching the document PDF from source.</summary>
    Downloading,

    /// <summary>Stripping the diagonal text-overlay watermark.</summary>
    RemovingWatermark,

    /// <summary>Inspecting the cleaned PDF for its section/page layout.</summary>
    DetectingLayout,

    /// <summary>The document is prepared and waiting for a print decision.</summary>
    Ready,

    /// <summary>Printing the even-numbered pages (first pass).</summary>
    PrintingEvenPages,

    /// <summary>Even pass done; waiting for the operator to flip the paper stack.</summary>
    AwaitingFlip,

    /// <summary>Printing the odd-numbered pages (second pass).</summary>
    PrintingOddPages,

    /// <summary>The run finished successfully.</summary>
    Completed,

    /// <summary>The run failed.</summary>
    Failed,
}
