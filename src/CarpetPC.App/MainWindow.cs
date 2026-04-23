using CarpetPC.Core;
using CarpetPC.Core.Agent;
using CarpetPC.Core.Audio;
using CarpetPC.Core.Models;
using CarpetPC.Core.Safety;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CarpetPC.App;

public sealed class MainWindow : Window
{
    private readonly IRuntimeLog _runtimeLog;
    private readonly PauseState _pauseState;
    private readonly ResourceBudgetService _resourceBudgetService;
    private readonly ModelSetupService _modelSetupService;
    private readonly StubWakeWordService _wakeWordService;
    private readonly ISpeechTranscriber _speechTranscriber;
    private readonly AgentOrchestrator _agentOrchestrator;
    private readonly TextBox _logBox = new();
    private readonly TextBlock _statusText = new();
    private readonly Button _pauseButton = new();
    private CancellationTokenSource? _agentRun;
    private bool _allowClose;

    public MainWindow(
        IRuntimeLog runtimeLog,
        PauseState pauseState,
        ResourceBudgetService resourceBudgetService,
        ModelSetupService modelSetupService,
        StubWakeWordService wakeWordService,
        ISpeechTranscriber speechTranscriber,
        AgentOrchestrator agentOrchestrator)
    {
        _runtimeLog = runtimeLog;
        _pauseState = pauseState;
        _resourceBudgetService = resourceBudgetService;
        _modelSetupService = modelSetupService;
        _wakeWordService = wakeWordService;
        _speechTranscriber = speechTranscriber;
        _agentOrchestrator = agentOrchestrator;

        Title = "CarpetPC";
        Width = 820;
        Height = 560;
        MinWidth = 620;
        MinHeight = 420;
        Background = new SolidColorBrush(Color.FromRgb(18, 20, 22));
        Content = BuildContent();

        _runtimeLog.EntryWritten += OnLogEntry;
        _pauseState.Changed += (_, _) => Dispatcher.Invoke(UpdateStatus);
        _wakeWordService.WakeDetected += OnWakeDetected;
        Loaded += async (_, _) => await RefreshResourceStatusAsync();
        Closing += (_, e) =>
        {
            if (_allowClose)
            {
                return;
            }

            e.Cancel = true;
            Hide();
        };
    }

    public void RequestExit()
    {
        _allowClose = true;
        Close();
    }

    private UIElement BuildContent()
    {
        var root = new DockPanel { Margin = new Thickness(18) };
        var title = new TextBlock
        {
            Text = "CarpetPC",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        };

        DockPanel.SetDock(title, Dock.Top);
        root.Children.Add(title);

        _statusText.Foreground = Brushes.LightGray;
        _statusText.Margin = new Thickness(0, 0, 0, 12);
        DockPanel.SetDock(_statusText, Dock.Top);
        root.Children.Add(_statusText);

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _pauseButton.Content = "Pause";
        _pauseButton.Margin = new Thickness(0, 0, 8, 0);
        _pauseButton.Click += (_, _) =>
        {
            if (_pauseState.IsPaused)
            {
                _pauseState.Resume();
                _runtimeLog.Info("Assistant resumed.");
            }
            else
            {
                _pauseState.Pause();
                _runtimeLog.Warn("Assistant paused.");
            }
        };

        var simulateWakeButton = new Button
        {
            Content = "Simulate Hey Carpet",
            Margin = new Thickness(0, 0, 8, 0)
        };
        simulateWakeButton.Click += (_, _) => _wakeWordService.SimulateWake();

        var modelsButton = new Button
        {
            Content = "Model Setup",
            Margin = new Thickness(0, 0, 8, 0)
        };
        modelsButton.Click += (_, _) => ShowModelPrompt();

        var resourceButton = new Button
        {
            Content = "Refresh Resources"
        };
        resourceButton.Click += async (_, _) => await RefreshResourceStatusAsync();

        toolbar.Children.Add(_pauseButton);
        toolbar.Children.Add(simulateWakeButton);
        toolbar.Children.Add(modelsButton);
        toolbar.Children.Add(resourceButton);
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);

        _logBox.IsReadOnly = true;
        _logBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _logBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        _logBox.Background = new SolidColorBrush(Color.FromRgb(6, 7, 8));
        _logBox.Foreground = new SolidColorBrush(Color.FromRgb(187, 255, 204));
        _logBox.FontFamily = new FontFamily("Cascadia Mono");
        _logBox.TextWrapping = TextWrapping.NoWrap;
        root.Children.Add(_logBox);

        UpdateStatus();
        return root;
    }

    private async void OnWakeDetected(object? sender, WakeDetectedEventArgs e)
    {
        if (_pauseState.IsPaused)
        {
            _runtimeLog.Warn("Wake ignored because assistant is paused.");
            return;
        }

        _agentRun?.Cancel();
        _agentRun = new CancellationTokenSource();

        try
        {
            _runtimeLog.Info($"Wake detected: {e.Phrase}");
            var command = await _speechTranscriber.ListenForCommandAsync(_agentRun.Token);
            await _agentOrchestrator.RunCommandAsync(command.Text, _agentRun.Token);
        }
        catch (OperationCanceledException)
        {
            _runtimeLog.Warn("Agent run cancelled.");
        }
        catch (Exception ex)
        {
            _runtimeLog.Error(ex.Message);
        }
    }

    private void ShowModelPrompt()
    {
        var builder = new StringBuilder();
        builder.AppendLine("CarpetPC will never download models without confirmation.");
        builder.AppendLine();
        builder.AppendLine($"Model folder: {_modelSetupService.GetModelDirectory()}");
        builder.AppendLine();

        foreach (var item in _modelSetupService.GetAvailableModels())
        {
            builder.AppendLine($"- {item.DisplayName}");
            builder.AppendLine($"  Source: {item.SourceUri}");
            builder.AppendLine($"  Status: {(_modelSetupService.IsModelPresent(item) ? "Present" : "Missing")}");
        }

        MessageBox.Show(this, builder.ToString(), "Model Setup", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task RefreshResourceStatusAsync()
    {
        var snapshot = await _resourceBudgetService.CaptureAsync(CancellationToken.None);
        var profile = _resourceBudgetService.SelectProfile(snapshot);
        var freeVram = snapshot.FreeVramBytes is null
            ? "unknown"
            : $"{snapshot.FreeVramBytes.Value / 1024d / 1024d / 1024d:0.0} GiB";

        _runtimeLog.Info($"Resource profile: {profile}; free VRAM: {freeVram}; app RAM: {snapshot.WorkingSetBytes / 1024d / 1024d:0} MiB.");
        UpdateStatus();
    }

    private void OnLogEntry(object? sender, RuntimeLogEntry entry)
    {
        Dispatcher.Invoke(() =>
        {
            _logBox.AppendText($"[{entry.Timestamp:HH:mm:ss}] {entry.Level}: {entry.Message}{Environment.NewLine}");
            _logBox.ScrollToEnd();
        });
    }

    private void UpdateStatus()
    {
        _pauseButton.Content = _pauseState.IsPaused ? "Resume" : "Pause";
        _statusText.Text = _pauseState.IsPaused
            ? "Paused. Ctrl+Alt+Esc and voice stop keep the assistant halted."
            : "Listening for Hey Carpet. Models are only downloaded after explicit confirmation.";
    }
}
