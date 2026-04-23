using CarpetPC.Core.Observation;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Automation;

namespace CarpetPC.App.Observation;

public sealed class WindowsScreenObserver : IScreenObserver
{
    public Task<ScreenObservation> CaptureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var screenshot = CapturePrimaryScreenPng();
        var elements = CaptureUiElements();
        var summary = $"Primary screen captured with {elements.Count} visible UI Automation elements.";

        return Task.FromResult(new ScreenObservation(DateTimeOffset.Now, summary, elements, screenshot));
    }

    private static byte[] CapturePrimaryScreenPng()
    {
        var width = (int)SystemParameters.PrimaryScreenWidth;
        var height = (int)SystemParameters.PrimaryScreenHeight;

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height));

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static IReadOnlyList<UiElementSnapshot> CaptureUiElements()
    {
        var snapshots = new List<UiElementSnapshot>();
        try
        {
            var root = AutomationElement.RootElement;
            var walker = TreeWalker.ControlViewWalker;
            var current = walker.GetFirstChild(root);

            while (current is not null && snapshots.Count < 80)
            {
                AddElement(current, snapshots);
                current = walker.GetNextSibling(current);
            }
        }
        catch
        {
            return snapshots;
        }

        return snapshots;
    }

    private static void AddElement(AutomationElement element, List<UiElementSnapshot> snapshots)
    {
        try
        {
            var rectangle = element.Current.BoundingRectangle;
            if (rectangle.IsEmpty || rectangle.Width <= 1 || rectangle.Height <= 1)
            {
                return;
            }

            snapshots.Add(new UiElementSnapshot(
                element.Current.Name ?? string.Empty,
                element.Current.ControlType?.ProgrammaticName ?? string.Empty,
                element.Current.AutomationId ?? string.Empty,
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                rectangle.Height));
        }
        catch
        {
            // Some windows deny automation reads; skip them and keep the observation usable.
        }
    }
}
