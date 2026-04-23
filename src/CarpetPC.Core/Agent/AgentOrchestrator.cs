using CarpetPC.Core.Audio;
using CarpetPC.Core.Observation;
using CarpetPC.Core.Safety;

namespace CarpetPC.Core.Agent;

public sealed class AgentOrchestrator(
    IModelRuntime modelRuntime,
    IScreenObserver screenObserver,
    IAutomationExecutor automationExecutor,
    AgentActionValidator validator,
    IRuntimeLog runtimeLog,
    PauseState pauseState)
{
    private const int MaxStepsPerCommand = 8;

    public async Task RunCommandAsync(string command, CancellationToken cancellationToken)
    {
        runtimeLog.Info($"Command: {command}");
        var progress = "No actions have been completed yet.";
        var transcripts = Array.Empty<TranscriptSegment>();

        for (var step = 1; step <= MaxStepsPerCommand; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pauseState.IsPaused)
            {
                runtimeLog.Warn("Agent paused before next action.");
                return;
            }

            var observation = await screenObserver.CaptureAsync(cancellationToken);
            var turn = new AgentTurn(command, progress, observation, transcripts);
            var action = await modelRuntime.GetNextActionAsync(turn, cancellationToken);
            var validation = validator.Validate(action, riskyActionsConfirmed: false);

            runtimeLog.Info($"Step {step}: {action.Summary}");

            if (validation.RequiresConfirmation)
            {
                runtimeLog.Warn(validation.Reason ?? "Action requires confirmation.");
                return;
            }

            if (!validation.IsAllowed)
            {
                runtimeLog.Error(validation.Reason ?? "Action blocked.");
                return;
            }

            if (action.Action is AgentActionKind.Finish or AgentActionKind.Abort)
            {
                runtimeLog.Info(action.Summary);
                return;
            }

            await automationExecutor.ExecuteAsync(action, cancellationToken);
            progress = action.Summary;
            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
        }

        runtimeLog.Warn("Agent stopped after reaching the maximum step count.");
    }
}
