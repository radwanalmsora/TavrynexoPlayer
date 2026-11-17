using System.Windows;
using LibVLCSharp.Shared;

namespace TavrynexoPlayer;

public partial class App : Application
{
    public static string? StartupFile { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Windows 10 (build 19041+) and Windows 11 only.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            MessageBox.Show("Tavrynexo Player requires Windows 10 (version 2004 or later) or Windows 11.",
                "Tavrynexo Player", MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown();
            return;
        }

        Core.Initialize();

        if (e.Args.Length > 0) StartupFile = e.Args[0];
        base.OnStartup(e);
    }
}
