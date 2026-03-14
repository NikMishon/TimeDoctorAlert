using System.Diagnostics;

namespace TimeDoctorAlert.Platform.Linux;

public class LinuxAudioPlayer : IAudioPlayer
{
    private string? _tempFilePath;
    private Process? _currentProcess;
    private readonly string _playerCommand;
    private readonly string _playerArgs;

    public LinuxAudioPlayer()
    {
        (_playerCommand, _playerArgs) = DetectPlayer();
    }

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");
        await File.WriteAllBytesAsync(_tempFilePath, Resources.GetMp3Bytes(), cancellationToken);

        var args = _playerArgs.Replace("{file}", _tempFilePath);

        while (!cancellationToken.IsCancellationRequested)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = _playerCommand,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
                break;

            _currentProcess = process;

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                KillAndDispose(process);
                throw;
            }

            process.Dispose();
        }
    }

    private static void KillAndDispose(Process? process)
    {
        if (process == null) return;
        try { process.Kill(); } catch { /* already exited */ }
        process.Dispose();
    }

    public void Dispose()
    {
        KillAndDispose(_currentProcess);
        _currentProcess = null;

        if (_tempFilePath != null && File.Exists(_tempFilePath))
        {
            try { File.Delete(_tempFilePath); } catch { /* best effort */ }
        }
    }

    private static (string command, string args) DetectPlayer()
    {
        if (IsToolAvailable("ffplay"))
            return ("ffplay", "-nodisp -autoexit {file}");
        if (IsToolAvailable("mpg123"))
            return ("mpg123", "{file}");

        throw new InvalidOperationException(
            "ffplay or mpg123 is required for audio playback. " +
            "Install with: sudo apt install ffmpeg or sudo apt install mpg123");
    }

    private static bool IsToolAvailable(string tool)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = tool,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) return false;
            process.WaitForExit(3000);
            var result = process.ExitCode == 0;
            process.Dispose();
            return result;
        }
        catch
        {
            return false;
        }
    }
}
