using System.Diagnostics;
using Serilog;

namespace TimeDoctorAlert.Platform.Mac;

public class MacAudioPlayer : IAudioPlayer
{
    private string? _tempFilePath;
    private Process? _currentProcess;

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");
        var mp3Bytes = Resources.GetMp3Bytes();
        Log.Information("Audio: writing {Bytes} bytes to {Path}", mp3Bytes.Length, _tempFilePath);
        await File.WriteAllBytesAsync(_tempFilePath, mp3Bytes, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            Log.Information("Audio: starting afplay {Path}", _tempFilePath);
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "afplay",
                Arguments = _tempFilePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
            {
                Log.Error("Audio: afplay process failed to start");
                break;
            }

            _currentProcess = process;

            try
            {
                await process.WaitForExitAsync(cancellationToken);
                var exitCode = process.ExitCode;
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                Log.Information("Audio: afplay exited with code {ExitCode}, stderr: {Stderr}", exitCode, stderr);
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
}
