using Serilog;

namespace TimeDoctorAlert.Platform;

public class WindowTracker
{
    private readonly IWindowEnumerator _enumerator;
    private List<WindowInfo> _windows = new();

    public WindowTracker(IWindowEnumerator enumerator)
    {
        _enumerator = enumerator;
    }

    public int UpdateWindowList(Func<WindowInfo, bool> filter)
    {
        var allWindows = _enumerator.GetVisibleWindows();

        var snapshot = allWindows.Where(filter).ToList();

        var newWindows = snapshot
            .Where(w => _windows.All(w2 => w2.Id != w.Id))
            .ToList();

        var closedWindows = _windows
            .Where(w => snapshot.All(w2 => w2.Id != w.Id))
            .ToList();

        var changedWindows = snapshot
            .Where(w => _windows.Any(w2 => w2.Id == w.Id && w2.IsForeground != w.IsForeground))
            .ToList();

        if (newWindows.Count != 0 || closedWindows.Count != 0 || changedWindows.Count != 0)
        {
            foreach (var window in newWindows)
                LogWindow("OPEN", window);

            foreach (var window in closedWindows)
                LogWindow("CLOSE", window);

            foreach (var window in changedWindows)
                LogWindow("CHANGE", window);

            _windows = snapshot;

            Log.Information("Windows count: {Count}", _windows.Count);
        }

        return _windows.Count;
    }

    private static void LogWindow(string action, WindowInfo window)
    {
        Log.Information(
            "[{Action}] Title: {Title}, Process: {ProcessName}, Size: {Width}x{Height}, IsForeground: {IsForeground}",
            action, window.Title, window.ProcessName, window.Width, window.Height, window.IsForeground);
    }
}
