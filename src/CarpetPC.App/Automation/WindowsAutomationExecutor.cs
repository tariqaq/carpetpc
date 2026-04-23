using CarpetPC.Core;
using CarpetPC.Core.Agent;
using System.Diagnostics;

namespace CarpetPC.App.Automation;

public sealed class WindowsAutomationExecutor(IRuntimeLog runtimeLog) : IAutomationExecutor
{
    public Task ExecuteAsync(AgentAction action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (action.Action)
        {
            case AgentActionKind.OpenUrl:
                Process.Start(new ProcessStartInfo(action.Target) { UseShellExecute = true });
                break;
            case AgentActionKind.OpenApp:
                Process.Start(new ProcessStartInfo(action.Target) { UseShellExecute = true });
                break;
            case AgentActionKind.Wait:
                return Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            default:
                runtimeLog.Warn($"Executor stub skipped unsupported action: {action.Action}");
                break;
        }

        return Task.CompletedTask;
    }
}

