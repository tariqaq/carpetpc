using CarpetPC.Core;
using CarpetPC.Core.Safety;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CarpetPC.App.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 9001;
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint VkEscape = 0x1B;

    private readonly Window _window;
    private readonly PauseState _pauseState;
    private readonly IRuntimeLog _runtimeLog;
    private HwndSource? _source;

    public GlobalHotkeyService(Window window, PauseState pauseState, IRuntimeLog runtimeLog)
    {
        _window = window;
        _pauseState = pauseState;
        _runtimeLog = runtimeLog;
        _window.SourceInitialized += OnSourceInitialized;
    }

    public void Dispose()
    {
        _source?.RemoveHook(WndProc);
        if (_source is not null)
        {
            UnregisterHotKey(_source.Handle, HotkeyId);
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _source = HwndSource.FromHwnd(new WindowInteropHelper(_window).Handle);
        _source?.AddHook(WndProc);

        if (_source is not null && RegisterHotKey(_source.Handle, HotkeyId, ModControl | ModAlt, VkEscape))
        {
            _runtimeLog.Info("Registered emergency pause hotkey: Ctrl+Alt+Esc.");
        }
        else
        {
            _runtimeLog.Warn("Could not register Ctrl+Alt+Esc. Another app may already own it.");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _pauseState.Pause();
            _runtimeLog.Warn("Emergency pause triggered by Ctrl+Alt+Esc.");
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

