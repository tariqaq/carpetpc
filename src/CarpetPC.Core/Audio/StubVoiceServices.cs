namespace CarpetPC.Core.Audio;

public sealed class StubWakeWordService : IWakeWordService
{
    public event EventHandler<WakeDetectedEventArgs>? WakeDetected;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void SimulateWake() => WakeDetected?.Invoke(this, new WakeDetectedEventArgs("Hey Carpet", DateTimeOffset.Now));
}

public sealed class StubSpeechTranscriber : ISpeechTranscriber
{
    public Task<TranscriptSegment> ListenForCommandAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        return Task.FromResult(new TranscriptSegment(
            "open Web WhatsApp in Brave",
            0.90,
            now.AddSeconds(-2),
            now));
    }
}

