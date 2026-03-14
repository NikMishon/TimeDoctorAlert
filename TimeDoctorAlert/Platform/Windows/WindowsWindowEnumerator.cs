using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TimeDoctorAlert.Platform.Windows;

public class WindowsWindowEnumerator : IWindowEnumerator
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public List<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        var foreground = GetForegroundWindow();

        EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
        {
            try
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                var sb = new StringBuilder(GetWindowTextLength(hWnd) + 1);
                GetWindowText(hWnd, sb, sb.Capacity);

                GetWindowRect(hWnd, out RECT rect);
                GetWindowThreadProcessId(hWnd, out var processId);

                using var process = Process.GetProcessById((int)processId);

                windows.Add(new WindowInfo
                {
                    Id = hWnd.ToString(),
                    Title = sb.ToString(),
                    ProcessName = process.ProcessName,
                    Width = rect.Right - rect.Left,
                    Height = rect.Bottom - rect.Top,
                    IsForeground = hWnd == foreground
                });
            }
            catch
            {
                // Process may have exited between enumeration and GetProcessById
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }
}
