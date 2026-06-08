namespace DocPrinter.Agent.Abstractions;

/// <summary>
/// A live, per-conversation view of the current <see cref="PipelineActivity"/>. The pipeline, the
/// plugin, and the flip prompt push updates through <see cref="IProgress{T}.Report"/>; a UI reads
/// <see cref="Current"/> and subscribes to <see cref="Changed"/> to re-render. The host registers
/// the implementation (scoped per circuit in the Blazor app, a no-op in the console).
/// </summary>
public interface IPipelineStatus : IProgress<PipelineActivity>
{
    /// <summary>The most recently reported activity.</summary>
    PipelineActivity Current { get; }

    /// <summary>Raised after <see cref="Current"/> changes.</summary>
    event Action? Changed;
}
