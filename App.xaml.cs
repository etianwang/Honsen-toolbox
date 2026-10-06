using System.IO;

namespace HonsenToolbox;

public partial class App : System.Windows.Application
{
    public App()
    {
        DispatcherUnhandledException += (_, eventArgs) => WriteCrash(eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) => WriteCrash(eventArgs.ExceptionObject as Exception ?? new Exception(eventArgs.ExceptionObject?.ToString()));
    }

    private static void WriteCrash(Exception exception)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HonsenToolbox");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "debug-crash.log"), exception.ToString());
    }
}
