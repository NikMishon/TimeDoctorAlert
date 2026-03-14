using Serilog;

namespace TimeDoctorAlert.Platform.Linux;

public class LinuxTrayIcon : ITrayIcon
{
#pragma warning disable CS0067
    public event Action? OnExitClicked;
#pragma warning restore CS0067

    public void Show() => Log.Information("Tray icon not supported on Linux. Use Ctrl+C to exit.");

    public void Hide() { }

    public void Dispose() { }
}
