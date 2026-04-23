namespace CarpetPC.App.Audio;

public sealed class MicrophoneSelection
{
    public int? SelectedDeviceNumber { get; private set; }

    public string? SelectedDeviceName { get; private set; }

    public void SetSelected(MicrophoneDevice device)
    {
        SelectedDeviceNumber = device.DeviceNumber;
        SelectedDeviceName = device.Name;
    }
}

