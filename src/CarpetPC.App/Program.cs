using System.Windows;
using WpfApplication = System.Windows.Application;

namespace CarpetPC.App;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var application = new WpfApplication
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };

        var host = new AppHost(application);
        host.Start();
        application.Run();
        host.Dispose();
    }
}
