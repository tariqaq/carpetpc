namespace CarpetPC.Core.Audio;

public interface IWakeWordService
{
    event EventHandler<WakeDetectedEventArgs>? WakeDetected;

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

public interface ISpeechTranscriber
{
    Task<TranscriptSegment> ListenForCommandAsync(CancellationToken cancellationToken);
}

public sealed record WakeDetectedEventArgs(string Phrase, DateTimeOffset DetectedAt);

public sealed record TranscriptSegment(string Text, double Confidence, DateTimeOffset StartedAt, DateTimeOffset EndedAt);

