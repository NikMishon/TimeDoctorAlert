using System.Drawing;
using System.Windows.Forms;

namespace TimeDoctorAlert.Platform.Windows;

public class WindowsTrayIcon : ITrayIcon
{
    private readonly NotifyIcon _notifyIcon;

    public event Action? OnExitClicked;

    public WindowsTrayIcon()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Exit", null, (_, _) => OnExitClicked?.Invoke());

        using var iconStream = Resources.GetIconStream();
        _notifyIcon = new NotifyIcon
        {
            Icon = new Icon(iconStream),
            ContextMenuStrip = contextMenu
        };
    }

    public void Show() => _notifyIcon.Visible = true;

    public void Hide() => _notifyIcon.Visible = false;

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
