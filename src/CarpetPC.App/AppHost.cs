using CarpetPC.App.Automation;
using CarpetPC.App.Hotkeys;
using CarpetPC.App.Runtime;
using CarpetPC.App.Tray;
using CarpetPC.Core;
using CarpetPC.Core.Agent;
using CarpetPC.Core.Audio;
using CarpetPC.Core.Models;
using CarpetPC.Core.Observation;
using CarpetPC.Core.Safety;
using WpfApplication = System.Windows.Application;

namespace CarpetPC.App;

public sealed class AppHost : IDisposable
{
    private readonly WpfApplication _application;
    private readonly RuntimeLog _runtimeLog = new();
    private readonly PauseState _pauseState = new();
    private readonly CarpetPaths _paths = new();
    private MainWindow? _mainWindow;
    private TrayIconService? _trayIcon;
    private GlobalHotkeyService? _hotkeys;

    public AppHost(WpfApplication application)
    {
        _application = application;
    }

    public void Start()
    {
        _paths.EnsureCreated();
        var wakeWord = new StubWakeWordService();
        var transcriber = new StubSpeechTranscriber();
        var modelRuntime = new StubModelRuntime(_runtimeLog);
        var screenObserver = new StubScreenObserver();
        var executor = new WindowsAutomationExecutor(_runtimeLog);
        var orchestrator = new AgentOrchestrator(
            modelRuntime,
            screenObserver,
            executor,
            new AgentActionValidator(),
            _runtimeLog,
            _pauseState);

        _mainWindow = new MainWindow(
            _runtimeLog,
            _pauseState,
            new ResourceBudgetService(),
            new ModelSetupService(new ModelCatalog(), _paths),
            wakeWord,
            transcriber,
            orchestrator);

        _trayIcon = new TrayIconService(_mainWindow, _application, _pauseState, _runtimeLog);
        _hotkeys = new GlobalHotkeyService(_mainWindow, _pauseState, _runtimeLog);
        _mainWindow.Show();
        _trayIcon.Show();
    }

    public void Dispose()
    {
        _hotkeys?.Dispose();
        _trayIcon?.Dispose();
    }
}
