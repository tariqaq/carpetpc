namespace CarpetPC.Core.Observation;

public sealed class StubScreenObserver : IScreenObserver
{
    public Task<ScreenObservation> CaptureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var observation = new ScreenObservation(
            DateTimeOffset.Now,
            "Stub observation: screenshot and UI Automation capture are not wired yet.",
            [],
            null);

        return Task.FromResult(observation);
    }
}

