using CarpetPC.Core.Audio;
using CarpetPC.Core.Observation;

namespace CarpetPC.Core.Agent;

public interface IModelRuntime
{
    bool IsLoaded { get; }

    bool IsLoading { get; }

    RuntimeProfile ActiveProfile { get; }

    string StatusMessage { get; }

    Task LoadAsync(RuntimeProfile profile, CancellationToken cancellationToken);

    Task UnloadAsync(CancellationToken cancellationToken);

    Task<AgentAction> GetNextActionAsync(AgentTurn turn, CancellationToken cancellationToken);
}

public interface IAutomationExecutor
{
    Task ExecuteAsync(AgentAction action, CancellationToken cancellationToken);
}

public sealed record AgentTurn(
    string UserCommand,
    string ProgressSummary,
    ScreenObservation Observation,
    IReadOnlyList<TranscriptSegment> RecentTranscripts);
