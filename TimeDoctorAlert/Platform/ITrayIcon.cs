namespace TimeDoctorAlert.Platform;

public interface ITrayIcon : IDisposable
{
    void Show();
    void Hide();
    event Action? OnExitClicked;
}
