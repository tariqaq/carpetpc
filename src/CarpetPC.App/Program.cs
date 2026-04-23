using System.Windows;

namespace CarpetPC.App;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var application = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };

        var host = new AppHost(application);
        host.Start();
        application.Run();
        host.Dispose();
    }
}

