namespace CarpetPC.Core;

public sealed class RuntimeLog : IRuntimeLog
{
    private readonly object _fileLock = new();
    private readonly string? _logFilePath;

    public RuntimeLog(string? logDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            return;
        }

        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, $"carpetpc-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.log");
    }

    public event EventHandler<RuntimeLogEntry>? EntryWritten;

    public void Info(string message) => Write(RuntimeLogLevel.Info, message);

    public void Warn(string message) => Write(RuntimeLogLevel.Warning, message);

    public void Error(string message) => Write(RuntimeLogLevel.Error, message);

    private void Write(RuntimeLogLevel level, string message)
    {
        var entry = new RuntimeLogEntry(DateTimeOffset.Now, level, message);
        WriteFile(entry);
        EntryWritten?.Invoke(this, entry);
    }

    private void WriteFile(RuntimeLogEntry entry)
    {
        if (_logFilePath is null)
        {
            return;
        }

        lock (_fileLock)
        {
            File.AppendAllText(
                _logFilePath,
                $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] {entry.Level}: {entry.Message}{Environment.NewLine}");
        }
    }
}
