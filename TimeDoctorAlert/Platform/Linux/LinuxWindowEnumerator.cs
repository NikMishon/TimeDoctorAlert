using System.Diagnostics;

namespace TimeDoctorAlert.Platform.Linux;

public class LinuxWindowEnumerator : IWindowEnumerator
{
    public LinuxWindowEnumerator()
    {
        ValidateTool("wmctrl", "sudo apt install wmctrl");
    }

    public List<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        var activeWindowId = GetActiveWindowId();

        var output = RunCommand("wmctrl", "-l -G -p");
        if (string.IsNullOrEmpty(output))
            return windows;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var info = ParseWmctrlLine(line, activeWindowId);
                if (info != null)
                    windows.Add(info);
            }
            catch
            {
                // Skip lines that can't be parsed
            }
        }

        return windows;
    }

    private static WindowInfo? ParseWmctrlLine(string line, string? activeWindowId)
    {
        // Format: 0x{wid}  {desktop}  {pid}  {x}  {y}  {width}  {height}  {hostname}  {title...}
        var parts = line.Split((char[]?)null, 9, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 9)
            return null;

        var wid = parts[0];
        var pid = parts[2];
        var width = int.Parse(parts[5]);
        var height = int.Parse(parts[6]);
        var title = parts[8];

        var processName = GetProcessName(pid);
        if (string.IsNullOrEmpty(processName))
            return null;

        return new WindowInfo
        {
            Id = wid,
            Title = title,
            ProcessName = processName,
            Width = width,
            Height = height,
            IsForeground = wid == activeWindowId
        };
    }

    private static string GetProcessName(string pid)
    {
        try
        {
            return File.ReadAllText($"/proc/{pid}/comm").Trim();
        }
        catch
        {
            return "";
        }
    }

    private static string? GetActiveWindowId()
    {
        try
        {
            var output = RunCommand("xdotool", "getactivewindow");
            if (string.IsNullOrEmpty(output))
                return null;

            // xdotool returns decimal, wmctrl uses hex
            if (long.TryParse(output.Trim(), out var decId))
                return $"0x{decId:x8}";
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string RunCommand(string command, string arguments)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) return "";

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return output;
        }
        catch
        {
            return "";
        }
    }

    private static void ValidateTool(string tool, string installHint)
    {
        var result = RunCommand("which", tool);
        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException($"{tool} is required but not found. Install with: {installHint}");
    }
}
