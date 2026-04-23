namespace CarpetPC.Core;

public sealed class RuntimeLog : IRuntimeLog
{
    public event EventHandler<RuntimeLogEntry>? EntryWritten;

    public void Info(string message) => Write(RuntimeLogLevel.Info, message);

    public void Warn(string message) => Write(RuntimeLogLevel.Warning, message);

    public void Error(string message) => Write(RuntimeLogLevel.Error, message);

    private void Write(RuntimeLogLevel level, string message)
    {
        EntryWritten?.Invoke(this, new RuntimeLogEntry(DateTimeOffset.Now, level, message));
    }
}

