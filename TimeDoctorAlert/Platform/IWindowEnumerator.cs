namespace TimeDoctorAlert.Platform;

public interface IWindowEnumerator
{
    List<WindowInfo> GetVisibleWindows();
}
