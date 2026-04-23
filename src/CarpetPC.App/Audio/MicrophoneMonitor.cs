using NAudio.Wave;

namespace CarpetPC.App.Audio;

public sealed record MicrophoneDevice(int DeviceNumber, string Name);

public sealed class MicrophoneMonitor : IDisposable
{
    private WaveInEvent? _waveIn;

    public event EventHandler<float>? LevelChanged;

    public IReadOnlyList<MicrophoneDevice> GetDevices()
    {
        var devices = new List<MicrophoneDevice>();
        for (var i = 0; i < WaveIn.DeviceCount; i++)
        {
            devices.Add(new MicrophoneDevice(i, WaveIn.GetCapabilities(i).ProductName));
        }

        return devices;
    }

    public void Start(int deviceNumber)
    {
        Stop();

        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(16_000, 16, 1),
            BufferMilliseconds = 50
        };

        _waveIn.DataAvailable += (_, e) =>
        {
            float max = 0;
            for (var index = 0; index < e.BytesRecorded; index += 2)
            {
                var sample = BitConverter.ToInt16(e.Buffer, index) / 32768f;
                max = Math.Max(max, Math.Abs(sample));
            }

            LevelChanged?.Invoke(this, max);
        };

        _waveIn.StartRecording();
    }

    public void Stop()
    {
        if (_waveIn is null)
        {
            return;
        }

        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
    }

    public void Dispose() => Stop();
}
