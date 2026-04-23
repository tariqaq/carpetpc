using CarpetPC.Core;
using CarpetPC.Core.Agent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Forms = System.Windows.Forms;

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
            case AgentActionKind.Type:
                Forms.SendKeys.SendWait(action.Text ?? string.Empty);
                break;
            case AgentActionKind.KeyPress:
                Forms.SendKeys.SendWait(action.Text ?? action.Target);
                break;
            case AgentActionKind.Click:
                if (TryParsePoint(action.Target, out var x, out var y))
                {
                    SetCursorPos(x, y);
                    MouseClick();
                }
                else
                {
                    runtimeLog.Warn($"Click target is not a coordinate yet: {action.Target}");
                }

                break;
            default:
                runtimeLog.Warn($"Executor stub skipped unsupported action: {action.Action}");
                break;
        }

        return Task.CompletedTask;
    }

    private static bool TryParsePoint(string target, out int x, out int y)
    {
        var match = Regex.Match(target, @"(?<x>\d+)\s*,\s*(?<y>\d+)");
        if (match.Success)
        {
            x = int.Parse(match.Groups["x"].Value);
            y = int.Parse(match.Groups["y"].Value);
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    private static void MouseClick()
    {
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
}
