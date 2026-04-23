namespace CarpetPC.Core;

public sealed class CarpetPaths
{
    public string ConfigDirectory { get; }

    public string ModelDirectory { get; }

    public string LogDirectory { get; }

    public string RuntimeDirectory { get; }

    public string WakeTrainingDirectory { get; }

    public CarpetPaths()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        ConfigDirectory = Path.Combine(appData, "CarpetPC");
        ModelDirectory = Path.Combine(localAppData, "CarpetPC", "Models");
        LogDirectory = Path.Combine(localAppData, "CarpetPC", "Logs");
        RuntimeDirectory = Path.Combine(localAppData, "CarpetPC", "Runtimes");
        WakeTrainingDirectory = Path.Combine(localAppData, "CarpetPC", "WakeTraining");
    }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(ModelDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(RuntimeDirectory);
        Directory.CreateDirectory(WakeTrainingDirectory);
    }
}
