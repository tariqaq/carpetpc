using CarpetPC.Core;
using CarpetPC.Core.Agent;

namespace CarpetPC.App.Runtime;

public sealed class StubModelRuntime(IRuntimeLog runtimeLog) : IModelRuntime
{
    public bool IsLoaded { get; private set; }

    public bool IsLoading { get; private set; }

    public RuntimeProfile ActiveProfile { get; private set; } = RuntimeProfile.CpuSafe;

    public string StatusMessage { get; private set; } = "Stub model not loaded.";

    public bool VisionModeEnabled { get; set; }

    public Task LoadAsync(RuntimeProfile profile, CancellationToken cancellationToken)
    {
        IsLoading = false;
        IsLoaded = true;
        ActiveProfile = profile;
        StatusMessage = $"Stub model loaded with profile {profile}.";
        runtimeLog.Info($"Stub model loaded with profile {profile}.");
        return Task.CompletedTask;
    }

    public Task UnloadAsync(CancellationToken cancellationToken)
    {
        IsLoading = false;
        IsLoaded = false;
        StatusMessage = "Stub model unloaded.";
        runtimeLog.Info("Stub model unloaded.");
        return Task.CompletedTask;
    }

    public Task<AgentAction> GetNextActionAsync(AgentTurn turn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (turn.UserCommand.Contains("whatsapp", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new AgentAction(
                AgentActionKind.OpenUrl,
                "https://web.whatsapp.com/",
                null,
                0.85,
                RiskLevel.Low,
                "Open Web WhatsApp in the default browser."));
        }

        return Task.FromResult(new AgentAction(
            AgentActionKind.Finish,
            string.Empty,
            null,
            0.95,
            RiskLevel.Low,
            "No stub action matched; finishing."));
    }
}
