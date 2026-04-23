namespace CarpetPC.Core.Observation;

public interface IScreenObserver
{
    Task<ScreenObservation> CaptureAsync(CancellationToken cancellationToken);
}

public sealed record ScreenObservation(
    DateTimeOffset CapturedAt,
    string DisplaySummary,
    IReadOnlyList<UiElementSnapshot> UiElements,
    byte[]? ScreenshotPng);

public sealed record UiElementSnapshot(
    string Name,
    string ControlType,
    string AutomationId,
    double X,
    double Y,
    double Width,
    double Height);

