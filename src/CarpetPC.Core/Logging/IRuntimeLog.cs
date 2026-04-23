namespace CarpetPC.Core;

public interface IRuntimeLog
{
    event EventHandler<RuntimeLogEntry>? EntryWritten;

    void Info(string message);

    void Warn(string message);

    void Error(string message);
}

public enum RuntimeLogLevel
{
    Info,
    Warning,
    Error
}

public sealed record RuntimeLogEntry(DateTimeOffset Timestamp, RuntimeLogLevel Level, string Message);

