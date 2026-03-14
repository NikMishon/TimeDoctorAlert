using System.Runtime.InteropServices;
using Serilog;
using TimeDoctorAlert;
using TimeDoctorAlert.Platform;

var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
var logFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

Log.Logger = new LoggerConfiguration()
    .WriteTo.Seq("http://seq.n2home.keenetic.link")
    .WriteTo.Console()
    .WriteTo.File(logFile)
    .CreateLogger();

try
{
    Log.Information("TimeDoctorAlert starting on {Platform}", RuntimeInformation.OSDescription);

    var enumerator = PlatformFactory.CreateWindowEnumerator();
    var tracker = new WindowTracker(enumerator);
    using var audioPlayer = PlatformFactory.CreateAudioPlayer();
    using var trayIcon = PlatformFactory.CreateTrayIcon();
    using var cts = new CancellationTokenSource();

    Func<WindowInfo, bool> filter = w =>
        (w.ProcessName.Contains("time doctor", StringComparison.OrdinalIgnoreCase) ||
         w.ProcessName.Contains("timedoctor", StringComparison.OrdinalIgnoreCase)) &&
        w.Width > 550 &&
        w.Height > 100;

    var service = new WindowMonitorService(tracker, audioPlayer, filter);

    // Graceful shutdown — Console.CancelKeyPress is primary on all platforms
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
#if WINDOWS
        System.Windows.Forms.Application.ExitThread();
#endif
    };

    // SIGTERM/SIGINT handler for daemon mode (systemd, launchd, etc.)
    AppDomain.CurrentDomain.ProcessExit += (_, _) =>
    {
        cts.Cancel();
#if WINDOWS
        System.Windows.Forms.Application.ExitThread();
#endif
    };

    trayIcon.OnExitClicked += () =>
    {
        cts.Cancel();
#if WINDOWS
        System.Windows.Forms.Application.ExitThread();
#endif
    };

    var monitorTask = Task.Run(() => service.RunAsync(cts.Token));

    trayIcon.Show();

#if WINDOWS
    // Windows needs a message pump for NotifyIcon
    System.Windows.Forms.Application.Run(new System.Windows.Forms.ApplicationContext());
#else
    // On macOS/Linux just wait for the monitor to finish
    try
    {
        monitorTask.GetAwaiter().GetResult();
    }
    catch (OperationCanceledException) { }
#endif

    cts.Cancel();
    try { monitorTask.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }

    Log.Information("TimeDoctorAlert stopped");
}
catch (Exception ex)
{
    Log.Fatal(ex, "TimeDoctorAlert terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
