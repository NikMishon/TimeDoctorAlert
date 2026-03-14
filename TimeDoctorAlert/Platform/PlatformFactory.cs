using System.Runtime.InteropServices;

namespace TimeDoctorAlert.Platform;

public static class PlatformFactory
{
    public static IWindowEnumerator CreateWindowEnumerator()
    {
#if WINDOWS
        return new Windows.WindowsWindowEnumerator();
#elif MACOS
        return new Mac.MacWindowEnumerator();
#elif LINUX
        return new Linux.LinuxWindowEnumerator();
#else
        throw new PlatformNotSupportedException($"Platform not supported: {RuntimeInformation.OSDescription}");
#endif
    }

    public static IAudioPlayer CreateAudioPlayer()
    {
#if WINDOWS
        return new Windows.WindowsAudioPlayer();
#elif MACOS
        return new Mac.MacAudioPlayer();
#elif LINUX
        return new Linux.LinuxAudioPlayer();
#else
        throw new PlatformNotSupportedException($"Platform not supported: {RuntimeInformation.OSDescription}");
#endif
    }

    public static ITrayIcon CreateTrayIcon()
    {
#if WINDOWS
        return new Windows.WindowsTrayIcon();
#elif MACOS
        return new Mac.MacTrayIcon();
#elif LINUX
        return new Linux.LinuxTrayIcon();
#else
        throw new PlatformNotSupportedException($"Platform not supported: {RuntimeInformation.OSDescription}");
#endif
    }
}
