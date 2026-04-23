using CarpetPC.Core;
using CarpetPC.Core.Agent;
using CarpetPC.Core.Models;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarpetPC.App.Runtime;

public sealed class LlamaCppModelRuntime(
    ModelSetupService modelSetupService,
    ResourceBudgetService resourceBudgetService,
    IRuntimeLog runtimeLog) : IModelRuntime, IDisposable
{
    private readonly HttpClient _httpClient = new();
    private Process? _serverProcess;

    public bool IsLoaded { get; private set; }

    public RuntimeProfile ActiveProfile { get; private set; } = RuntimeProfile.WakeOnly;

    public async Task LoadAsync(RuntimeProfile profile, CancellationToken cancellationToken)
    {
        if (IsLoaded && ActiveProfile == profile)
        {
            return;
        }

        await UnloadAsync(cancellationToken);

        var llamaServer = FindLlamaServer();
        var modelPath = FindAgentModel();
        if (llamaServer is null || modelPath is null)
        {
            runtimeLog.Warn("Cannot load LLM: llama-server.exe or Gemma GGUF model is missing.");
            return;
        }

        var gpuLayers = profile switch
        {
            RuntimeProfile.High => 99,
            RuntimeProfile.Balanced => 28,
            RuntimeProfile.CpuSafe => 0,
            _ => 0
        };

        _serverProcess = Process.Start(new ProcessStartInfo
        {
            FileName = llamaServer,
            Arguments = $"-m \"{modelPath}\" --host 127.0.0.1 --port 39287 -c 4096 -ngl {gpuLayers}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        });

        ActiveProfile = profile;
        IsLoaded = _serverProcess is not null;
        runtimeLog.Info($"Started llama.cpp server with profile {profile}.");
        await WaitForServerAsync(cancellationToken);
    }

    public Task UnloadAsync(CancellationToken cancellationToken)
    {
        if (_serverProcess is { HasExited: false })
        {
            _serverProcess.Kill(entireProcessTree: true);
            runtimeLog.Info("Stopped llama.cpp server.");
        }

        _serverProcess?.Dispose();
        _serverProcess = null;
        IsLoaded = false;
        return Task.CompletedTask;
    }

    public async Task<AgentAction> GetNextActionAsync(AgentTurn turn, CancellationToken cancellationToken)
    {
        var snapshot = await resourceBudgetService.CaptureAsync(cancellationToken);
        var desiredProfile = resourceBudgetService.SelectProfile(snapshot);
        await LoadAsync(desiredProfile, cancellationToken);

        if (!IsLoaded)
        {
            return new AgentAction(
                AgentActionKind.Abort,
                string.Empty,
                null,
                1,
                RiskLevel.Low,
                "LLM runtime is not installed or not loaded. Open Model Setup.");
        }

        var prompt = BuildPrompt(turn);
        var response = await _httpClient.PostAsJsonAsync(
            "http://127.0.0.1:39287/completion",
            new
            {
                prompt,
                temperature = 0.1,
                n_predict = 256
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var completion = await response.Content.ReadFromJsonAsync<LlamaCompletionResponse>(cancellationToken);
        return ParseAction(completion?.Content ?? string.Empty);
    }

    public void Dispose()
    {
        _ = UnloadAsync(CancellationToken.None);
        _httpClient.Dispose();
    }

    private string? FindLlamaServer()
    {
        var runtimeRoot = modelSetupService.GetRuntimeDirectory();
        return Directory.Exists(runtimeRoot)
            ? Directory.EnumerateFiles(runtimeRoot, "llama-server.exe", SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    private string? FindAgentModel()
    {
        var item = modelSetupService.GetAvailableModels().FirstOrDefault(model => model.Kind == ModelAssetKind.AgentModel);
        return item is not null && modelSetupService.IsModelPresent(item) ? modelSetupService.GetAssetPath(item) : null;
    }

    private async Task WaitForServerAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var response = await _httpClient.GetAsync("http://127.0.0.1:39287/health", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        runtimeLog.Warn("llama.cpp server did not report healthy within the startup window.");
    }

    private static string BuildPrompt(AgentTurn turn)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are CarpetPC, a local Windows desktop control agent.");
        builder.AppendLine("Return only one JSON object with: action,target,text,confidence,riskLevel,summary.");
        builder.AppendLine("Allowed actions: open_url, open_app, click, type, keypress, wait, ask_confirmation, finish, abort.");
        builder.AppendLine("Risk levels: Low, Medium, High. Use High for purchases, installs, deletes, account changes, sending messages, or uncertain clicks.");
        builder.AppendLine();
        builder.AppendLine($"User command: {turn.UserCommand}");
        builder.AppendLine($"Progress: {turn.ProgressSummary}");
        builder.AppendLine($"Screen: {turn.Observation.DisplaySummary}");
        builder.AppendLine("UI elements:");

        foreach (var element in turn.Observation.UiElements.Take(40))
        {
            builder.AppendLine($"- {element.ControlType} \"{element.Name}\" id={element.AutomationId} rect={element.X:0},{element.Y:0},{element.Width:0},{element.Height:0}");
        }

        return builder.ToString();
    }

    private static AgentAction ParseAction(string content)
    {
        var jsonStart = content.IndexOf('{');
        var jsonEnd = content.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            return new AgentAction(AgentActionKind.Abort, string.Empty, null, 0.5, RiskLevel.Low, "Model did not return valid JSON.");
        }

        var json = content[jsonStart..(jsonEnd + 1)];
        var action = JsonSerializer.Deserialize<ModelActionDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (action is null)
        {
            return new AgentAction(AgentActionKind.Abort, string.Empty, null, 0.5, RiskLevel.Low, "Model returned an empty action.");
        }

        return new AgentAction(
            ParseKind(action.Action),
            action.Target ?? string.Empty,
            action.Text,
            action.Confidence,
            Enum.TryParse<RiskLevel>(action.RiskLevel, ignoreCase: true, out var risk) ? risk : RiskLevel.Medium,
            action.Summary ?? "Model selected an action.");
    }

    private static AgentActionKind ParseKind(string? action) => action?.ToLowerInvariant() switch
    {
        "open_url" => AgentActionKind.OpenUrl,
        "open_app" => AgentActionKind.OpenApp,
        "click" => AgentActionKind.Click,
        "type" => AgentActionKind.Type,
        "keypress" => AgentActionKind.KeyPress,
        "wait" => AgentActionKind.Wait,
        "ask_confirmation" => AgentActionKind.AskConfirmation,
        "finish" => AgentActionKind.Finish,
        _ => AgentActionKind.Abort
    };

    private sealed record LlamaCompletionResponse([property: JsonPropertyName("content")] string Content);

    private sealed record ModelActionDto(
        string? Action,
        string? Target,
        string? Text,
        double Confidence,
        string? RiskLevel,
        string? Summary);
}
