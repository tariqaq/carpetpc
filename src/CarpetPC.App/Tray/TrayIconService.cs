using CarpetPC.Core;
using CarpetPC.Core.Safety;
using Forms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;

namespace CarpetPC.App.Tray;

public sealed class TrayIconService : IDisposable
{
    private readonly WpfWindow _window;
    private readonly WpfApplication _application;
    private readonly PauseState _pauseState;
    private readonly IRuntimeLog _runtimeLog;
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayIconService(WpfWindow window, WpfApplication application, PauseState pauseState, IRuntimeLog runtimeLog)
    {
        _window = window;
        _application = application;
        _pauseState = pauseState;
        _runtimeLog = runtimeLog;
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "CarpetPC",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = false,
            ContextMenuStrip = BuildMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
    }

    public void Show() => _notifyIcon.Visible = true;

    public void Dispose() => _notifyIcon.Dispose();

    private Forms.ContextMenuStrip BuildMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open CarpetPC", null, (_, _) => ShowWindow());
        menu.Items.Add("Pause", null, (_, _) =>
        {
            _pauseState.Pause();
            _runtimeLog.Warn("Assistant paused from tray.");
        });
        menu.Items.Add("Resume", null, (_, _) =>
        {
            _pauseState.Resume();
            _runtimeLog.Info("Assistant resumed from tray.");
        });
        menu.Items.Add("Exit", null, (_, _) =>
        {
            _notifyIcon.Visible = false;
            if (_window is MainWindow mainWindow)
            {
                mainWindow.RequestExit();
            }

            _application.Shutdown();
        });
        return menu;
    }

    private void ShowWindow()
    {
        _window.Show();
        _window.Activate();
    }
}
