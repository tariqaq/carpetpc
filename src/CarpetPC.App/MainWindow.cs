using CarpetPC.App.Audio;
using CarpetPC.App.Models;
using CarpetPC.Core;
using CarpetPC.Core.Agent;
using CarpetPC.Core.Audio;
using CarpetPC.Core.Models;
using CarpetPC.Core.Safety;
using System.Windows;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfDock = System.Windows.Controls.Dock;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfDockPanel = System.Windows.Controls.DockPanel;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextWrapping = System.Windows.TextWrapping;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace CarpetPC.App;

public sealed class MainWindow : Window
{
    private readonly IRuntimeLog _runtimeLog;
    private readonly PauseState _pauseState;
    private readonly ResourceBudgetService _resourceBudgetService;
    private readonly ModelSetupService _modelSetupService;
    private readonly MicrophoneMonitor _microphoneMonitor;
    private readonly StubWakeWordService _wakeWordService;
    private readonly ISpeechTranscriber _speechTranscriber;
    private readonly AgentOrchestrator _agentOrchestrator;
    private readonly WpfTextBox _logBox = new();
    private readonly WpfTextBlock _statusText = new();
    private readonly WpfButton _pauseButton = new();
    private readonly WpfComboBox _micSelector = new();
    private readonly WpfProgressBar _micLevel = new();
    private readonly WpfCheckBox _developerMode = new();
    private readonly WpfButton _simulateWakeButton = new();
    private CancellationTokenSource? _agentRun;
    private bool _allowClose;

    public MainWindow(
        IRuntimeLog runtimeLog,
        PauseState pauseState,
        ResourceBudgetService resourceBudgetService,
        ModelSetupService modelSetupService,
        MicrophoneMonitor microphoneMonitor,
        StubWakeWordService wakeWordService,
        ISpeechTranscriber speechTranscriber,
        AgentOrchestrator agentOrchestrator)
    {
        _runtimeLog = runtimeLog;
        _pauseState = pauseState;
        _resourceBudgetService = resourceBudgetService;
        _modelSetupService = modelSetupService;
        _microphoneMonitor = microphoneMonitor;
        _wakeWordService = wakeWordService;
        _speechTranscriber = speechTranscriber;
        _agentOrchestrator = agentOrchestrator;

        Title = "CarpetPC";
        Width = 820;
        Height = 560;
        MinWidth = 620;
        MinHeight = 420;
        Background = new WpfSolidColorBrush(WpfColor.FromRgb(18, 20, 22));
        Content = BuildContent();

        _runtimeLog.EntryWritten += OnLogEntry;
        _microphoneMonitor.LevelChanged += OnMicLevelChanged;
        _pauseState.Changed += (_, _) => Dispatcher.Invoke(UpdateStatus);
        _wakeWordService.WakeDetected += OnWakeDetected;
        Loaded += async (_, _) =>
        {
            LoadMicrophones();
            await RefreshResourceStatusAsync();
        };
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
        _microphoneMonitor.Dispose();
        _allowClose = true;
        Close();
    }

    private UIElement BuildContent()
    {
        var root = new WpfDockPanel { Margin = new Thickness(18) };
        var title = new WpfTextBlock
        {
            Text = "CarpetPC",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = WpfBrushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        };

        WpfDockPanel.SetDock(title, WpfDock.Top);
        root.Children.Add(title);

        _statusText.Foreground = WpfBrushes.LightGray;
        _statusText.Margin = new Thickness(0, 0, 0, 12);
        WpfDockPanel.SetDock(_statusText, WpfDock.Top);
        root.Children.Add(_statusText);

        var toolbar = new WpfStackPanel
        {
            Orientation = WpfOrientation.Horizontal,
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

        var modelsButton = new WpfButton
        {
            Content = "Model Setup",
            Margin = new Thickness(0, 0, 8, 0)
        };
        modelsButton.Click += (_, _) => ShowModelPrompt();

        var resourceButton = new WpfButton
        {
            Content = "Refresh Resources"
        };
        resourceButton.Click += async (_, _) => await RefreshResourceStatusAsync();

        toolbar.Children.Add(_pauseButton);
        toolbar.Children.Add(modelsButton);
        toolbar.Children.Add(resourceButton);
        WpfDockPanel.SetDock(toolbar, WpfDock.Top);
        root.Children.Add(toolbar);

        var micPanel = new WpfStackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };

        micPanel.Children.Add(new WpfTextBlock
        {
            Text = "Mic:",
            Foreground = WpfBrushes.White,
            Width = 36,
            VerticalAlignment = VerticalAlignment.Center
        });

        _micSelector.Width = 280;
        _micSelector.Margin = new Thickness(0, 0, 10, 0);
        _micSelector.SelectionChanged += (_, _) => StartSelectedMic();

        _micLevel.Width = 180;
        _micLevel.Height = 18;
        _micLevel.Maximum = 100;
        _micLevel.Margin = new Thickness(0, 0, 10, 0);

        _developerMode.Content = "Developer mode";
        _developerMode.Foreground = WpfBrushes.White;
        _developerMode.Margin = new Thickness(0, 0, 10, 0);
        _developerMode.Checked += (_, _) => _simulateWakeButton.Visibility = Visibility.Visible;
        _developerMode.Unchecked += (_, _) => _simulateWakeButton.Visibility = Visibility.Collapsed;

        _simulateWakeButton.Content = "Simulate Hey Carpet";
        _simulateWakeButton.Visibility = Visibility.Collapsed;
        _simulateWakeButton.Click += (_, _) => _wakeWordService.SimulateWake();

        micPanel.Children.Add(_micSelector);
        micPanel.Children.Add(_micLevel);
        micPanel.Children.Add(_developerMode);
        micPanel.Children.Add(_simulateWakeButton);
        WpfDockPanel.SetDock(micPanel, WpfDock.Top);
        root.Children.Add(micPanel);

        _logBox.IsReadOnly = true;
        _logBox.VerticalScrollBarVisibility = WpfScrollBarVisibility.Auto;
        _logBox.HorizontalScrollBarVisibility = WpfScrollBarVisibility.Auto;
        _logBox.Background = new WpfSolidColorBrush(WpfColor.FromRgb(6, 7, 8));
        _logBox.Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(187, 255, 204));
        _logBox.FontFamily = new WpfFontFamily("Cascadia Mono");
        _logBox.TextWrapping = WpfTextWrapping.NoWrap;
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
        var setupWindow = new ModelSetupWindow(_modelSetupService, _runtimeLog)
        {
            Owner = this
        };
        setupWindow.ShowDialog();
        UpdateStatus();
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
        if (_pauseState.IsPaused)
        {
            _statusText.Text = "Paused. Ctrl+Alt+Esc and voice stop keep the assistant halted.";
            return;
        }

        _statusText.Text = _modelSetupService.AreRequiredAssetsPresent()
            ? "Ready. Listening for Hey Carpet."
            : "Setup required. Open Model Setup and install required assets before CarpetPC can listen normally.";
    }

    private void LoadMicrophones()
    {
        _micSelector.Items.Clear();
        foreach (var device in _microphoneMonitor.GetDevices())
        {
            _micSelector.Items.Add(device);
        }

        _micSelector.DisplayMemberPath = nameof(MicrophoneDevice.Name);

        if (_micSelector.Items.Count > 0)
        {
            _micSelector.SelectedIndex = 0;
        }
        else
        {
            _runtimeLog.Warn("No microphone input devices were found.");
        }
    }

    private void StartSelectedMic()
    {
        if (_micSelector.SelectedItem is not MicrophoneDevice device)
        {
            return;
        }

        try
        {
            _microphoneMonitor.Start(device.DeviceNumber);
            _runtimeLog.Info($"Monitoring microphone: {device.Name}.");
        }
        catch (Exception ex)
        {
            _runtimeLog.Error($"Could not start microphone monitor: {ex.Message}");
        }
    }

    private void OnMicLevelChanged(object? sender, float level)
    {
        Dispatcher.Invoke(() => _micLevel.Value = Math.Clamp(level * 100, 0, 100));
    }
}
