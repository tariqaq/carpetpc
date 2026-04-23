using System.Diagnostics;
using System.Globalization;

namespace CarpetPC.Core;

public enum RuntimeProfile
{
    High,
    Balanced,
    CpuSafe,
    WakeOnly
}

public sealed record ResourceSnapshot(
    long? TotalVramBytes,
    long? FreeVramBytes,
    long WorkingSetBytes,
    DateTimeOffset CapturedAt);

public sealed class ResourceBudgetService
{
    private readonly TimeProvider _timeProvider;
    private RuntimeProfile _activeProfile = RuntimeProfile.CpuSafe;
    private DateTimeOffset _lastProfileChange;

    public ResourceBudgetService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _lastProfileChange = DateTimeOffset.MinValue;
    }

    public RuntimeProfile ActiveProfile => _activeProfile;

    public async Task<ResourceSnapshot> CaptureAsync(CancellationToken cancellationToken)
    {
        var (total, free) = await TryReadNvidiaVramAsync(cancellationToken);
        return new ResourceSnapshot(total, free, Environment.WorkingSet, _timeProvider.GetUtcNow());
    }

    public RuntimeProfile SelectProfile(ResourceSnapshot snapshot)
    {
        var now = _timeProvider.GetUtcNow();
        var desired = GetDesiredProfile(snapshot);
        var age = now - _lastProfileChange;

        if (desired == _activeProfile)
        {
            return _activeProfile;
        }

        var canPromote = desired < _activeProfile && age >= TimeSpan.FromSeconds(30);
        var canDemote = desired > _activeProfile && age >= TimeSpan.FromSeconds(5);

        if (canPromote || canDemote)
        {
            _activeProfile = desired;
            _lastProfileChange = now;
        }

        return _activeProfile;
    }

    private static RuntimeProfile GetDesiredProfile(ResourceSnapshot snapshot)
    {
        if (snapshot.WorkingSetBytes >= 6L * 1024L * 1024L * 1024L)
        {
            return RuntimeProfile.WakeOnly;
        }

        if (snapshot.FreeVramBytes is null)
        {
            return RuntimeProfile.CpuSafe;
        }

        var freeGiB = snapshot.FreeVramBytes.Value / 1024d / 1024d / 1024d;

        return freeGiB switch
        {
            >= 5.5 => RuntimeProfile.High,
            >= 3.0 => RuntimeProfile.Balanced,
            >= 1.5 => RuntimeProfile.CpuSafe,
            _ => RuntimeProfile.WakeOnly
        };
    }

    private static async Task<(long? TotalBytes, long? FreeBytes)> TryReadNvidiaVramAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=memory.total,memory.free --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return (null, null);
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var line = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (line is null)
            {
                return (null, null);
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                return (null, null);
            }

            var totalMiB = long.Parse(parts[0], CultureInfo.InvariantCulture);
            var freeMiB = long.Parse(parts[1], CultureInfo.InvariantCulture);
            return (totalMiB * 1024L * 1024L, freeMiB * 1024L * 1024L);
        }
        catch
        {
            return (null, null);
        }
    }
}
