using Serilog;

namespace TimeDoctorAlert.Platform.Mac;

public class MacTrayIcon : ITrayIcon
{
#pragma warning disable CS0067 // Event is never used (no-op tray implementation)
    public event Action? OnExitClicked;
#pragma warning restore CS0067

    public void Show() => Log.Information("Tray icon not supported on macOS. Use Ctrl+C to exit.");

    public void Hide() { }

    public void Dispose() { }
}
